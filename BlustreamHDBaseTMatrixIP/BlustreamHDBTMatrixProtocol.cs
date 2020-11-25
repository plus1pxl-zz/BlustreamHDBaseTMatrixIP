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

        public BlustreamHDBTMatrixProtocol(ISerialTransport transport, byte id)
            : base(transport, id)
        {
            base.PowerIsOn = true;
        }

        protected override void DeConstructSwitcherRoute(string response)
        {
        }

        public override void DataHandler(string rx)
        {
            try
            {
                string rxToLower = rx.ToLower();
                char delimiter = '\r';
                foreach (string str in rxToLower.Split(delimiter))
                {
                    string input = str.Trim();
                    if (!string.IsNullOrEmpty(input))
                    {
                        if (input.StartsWith("[error]"))
                        {
                            base.Log("Unable to switch: " + input);
                            continue;
                        }

                        if (input.StartsWith("="))
                        {
                            string pattern = @"^output.*(?:\r\n|[\r\n])(?:.*(?:\r\n|[\r\n]))*^audio";
                            string outputsInfoRx = Regex.Match(input, pattern, RegexOptions.Multiline).ToString();

                            DataHandler(outputsInfoRx);
                            if (outputsInfoRx.Length > 0)
                            {
                                List<string> outputsInfoArr = SplitByLine(outputsInfoRx).ToList();
                                outputsInfoArr.RemoveAt(outputsInfoArr.Count - 1);

                                string[] outputInfoKeys = SplitByTwoOrMoreWhiteSpaces(outputsInfoArr[0]).ToArray();
                                int numberOfOutputs = outputsInfoArr.Count - 1;
                                int numberOfProperties = outputInfoKeys.Length;

                                //Dictionary<string, string>[] outputsInfo = new Dictionary<string, string>[numberOfOutputs];
                                List<List<Dictionary<string, string>>> outputsInfo = new List<List<Dictionary<string, string>>>();

                                for (int i = 0; i < numberOfOutputs; i++)
                                {
                                    string[] outputInfoValues = SplitByTwoOrMoreWhiteSpaces(outputsInfoArr[i + 1]).ToArray();
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
                                    AudioVideoExtender outputExtender = this.GetExtenderByApiIdentifier(outputExtenderStr);
                                    AudioVideoExtender inputExtender = this.GetExtenderByApiIdentifier(inputExtenderStr);

                                    if (outputExtender != null)
                                    {
                                        outputExtender.VideoSourceExtenderId = inputExtender.Id;
                                    }
                                }
                            }

                            continue;
                        }

                        Match commandSuccess = new Regex(@"\[success\].+?(\d+).+?(\d+)").Match(input);

                        if (commandSuccess.Success)
                        {
                            AudioVideoExtender outputExtender = this.GetExtenderByApiIdentifier(commandSuccess.Groups[1].Value);
                            AudioVideoExtender inputExtender = this.GetExtenderByApiIdentifier(commandSuccess.Groups[2].Value);

                            if (outputExtender != null)
                            {
                                outputExtender.VideoSourceExtenderId = inputExtender.Id;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                base.Log(string.Concat("Unable to parse Blustream response: ", e.Message));
                base.Log(string.Concat("Response was: ", rx));
            }

            base.DataHandler(rx);
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

        static 

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
