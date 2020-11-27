using System.Text;
using Independentsoft.Exchange;

namespace AVSwitcherBlustreamHDBTMatrixIP
{
    using Crestron.RAD.Common.BasicDriver;
    using Crestron.RAD.Common.Enums;
    using Crestron.RAD.Common.Transports;
    using Crestron.RAD.DeviceTypes.AudioVideoSwitcher;
    using Crestron.RAD.DeviceTypes.AudioVideoSwitcher.Extender;

    using Crestron.SimplSharp;

    using System;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using System.Linq;

    public class BlustreamHDBTMatrixProtocol : AAudioVideoSwitcherProtocol
    {

        private StringBuilder sbResponse;

        public BlustreamHDBTMatrixProtocol(ISerialTransport transport, byte id)
            : base(transport, id)
        {
            base.PowerIsOn = true;
            base.PollingInterval = 20000;
            this.sbResponse = new StringBuilder("");
        }

        protected override ValidatedRxData ResponseValidator(string response, CommonCommandGroupType commandGroup)
        {
            return base.ResponseValidator(response, commandGroup);
        }

        public override void DataHandler(string rx)
        {

            if (!rx.Contains("SUCCESS") | !rx.Contains("ERROR"))
            {
                sbResponse.Append(rx);

                if (rx.Contains("MAC"))
                {
                    List<string> processedFeedback = ProcessResponse(sbResponse.ToString());
                    sbResponse.Clear();

                    foreach (var feedbackLine in processedFeedback)
                    {
                        base.DataHandler(feedbackLine);
                    }
                }
            }
        }

        private List<string> ProcessResponse(string input)
        {
            string inputToLower = input.ToLower();
            List<string> processedFeedback = new List<string>();

            try
            {
                string pattern = @"^output.*(?:\r\n|[\r\n])(?:.*(?:\r\n|[\r\n]))*^audio";
                string outputsInfoRx = Regex.Match(inputToLower, pattern, RegexOptions.Multiline).ToString();


                if (outputsInfoRx.Length > 0)
                {
                    List<string> outputsInfoRaw = SplitByLine(outputsInfoRx).ToList();
                    outputsInfoRaw.RemoveAt(outputsInfoRaw.Count - 1);

                    List<string> outputInfoKeys = SplitByTwoOrMoreWhiteSpaces(outputsInfoRaw[0]).ToList();
                    int numberOfOutputs = outputsInfoRaw.Count - 1;
                    int numberOfProperties = outputInfoKeys.Count;

                    List<List<Dictionary<string, string>>> outputsInfo = new List<List<Dictionary<string, string>>>();

                    for (int i = 0; i < numberOfOutputs; i++)
                    {
                        string[] outputInfoValues = SplitByTwoOrMoreWhiteSpaces(outputsInfoRaw[i + 1]).ToArray();
                        List<Dictionary<string, string>> outputInfo = new List<Dictionary<string, string>>();

                        for (int j = 0; j < numberOfProperties; j++)
                        {
                            Dictionary<string, string> keyValuePair = new Dictionary<string, string>();
                            keyValuePair.Add(outputInfoKeys[j], outputInfoValues[j]);
                            outputInfo.Add(keyValuePair);
                        }

                        outputsInfo.Add(outputInfo);
                    }

                    for (int i = 0; i < outputsInfo.Count; i++)
                    {
                        string outputExtenderStr = string.Empty;
                        string inputExtenderStr = string.Empty;

                        foreach (var outputInfo in outputsInfo[i])
                        {
                            if (outputInfo.ContainsKey("output"))
                            {
                                outputExtenderStr = outputInfo["output"];
                            }
                            else if (outputInfo.ContainsKey("fromin"))
                            {
                                inputExtenderStr = outputInfo["fromin"];
                            }
                        }

                        processedFeedback.Add($"->ROUTED={outputExtenderStr}:{inputExtenderStr}\r");
                    }
                }
            }
            catch (Exception exception)
            {
                CrestronConsole.PrintLine($"Unable to process feedback!!!");
                CrestronConsole.Print(exception.ToString());
            }

            return processedFeedback;
        }

        protected override void DeConstructSwitcherRoute(string response)
        {
            // Receiving: ROUTED=OUTPUT#1:INPUT#1
            //            ROUTED=<output ID>:<input ID>
            //            ROUTED= is stripped out of response before this is called. 

            var routePath = response.Split(':');
            AudioVideoExtender inputExtender = null;
            AudioVideoExtender outputExtender = null;

            // We can get the extender objects here using the API identifier set
            // in the embedded file.
            // We can also get the extender objects by their unique ID using GetExtenderById
            outputExtender = GetExtenderByApiIdentifier(routePath[0]);
            inputExtender = routePath.Length > 1 ? GetExtenderByApiIdentifier(routePath[1]) : null;

            // Figured out which input is routed to the specified output
            // Now update the output extender with the current source routed to it
            // The framework will figure out if this was a real change or not if it is not done here.
            if (outputExtender != null)
            {
                outputExtender.VideoSourceExtenderId = inputExtender == null ?
                    null : inputExtender.Id;
                CrestronConsole.PrintLine($"IN:{inputExtender.ApiIdentifier} routed to OUT{outputExtender.ApiIdentifier}");
            }
        }

        protected override bool PrepareStringThenSend(CommandSet commandSet)
        {
            if (!commandSet.CommandPrepared)
            {
                commandSet.Command = string.Concat(commandSet.Command, '\r');
                commandSet.CommandPrepared = true;
            }

            return base.PrepareStringThenSend(commandSet);
        }


        static IEnumerable<string> SplitByLine(string str)
        {
            return Regex
                .Split(str, @"(?:\r\n|[\r\n])")
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrEmpty(i));
        }

        static IEnumerable<string> SplitByTwoOrMoreWhiteSpaces(string str)
        {
            return Regex
                .Split(str, @"\s{2,}")
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrEmpty(i));
        }


    }
}
