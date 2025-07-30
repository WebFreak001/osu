// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
using System.Threading.Tasks;
using osu.Framework.Platform;

namespace osu.Game.IPC
{
    public class CommandIPCChannel : IpcChannel<CommandMessage>
    {
        private readonly OsuGame game;

        public CommandIPCChannel(IIpcHost host, OsuGame game = null)
            : base(host)
        {
            this.game = game;

            MessageReceived += msg =>
            {
                Debug.Assert(game != null);
                RunCommand(msg.Command, msg.Arguments).ContinueWith(t =>
                {
                    if (t.Exception != null) throw t.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted);

                return null;
            };
        }

        public async Task RunCommand(string command, string[] arguments)
        {
            if (game == null)
            {
                // we want to contact a remote osu! to handle the import.
                await SendMessageAsync(new CommandMessage { Command = command, Arguments = arguments }).ConfigureAwait(false);
                return;
            }

            switch (command)
            {
                case @"set-ruleset":
                    game.TrySetRuleset(arguments[0]);
                    break;

                case @"set-guest":
                    game.SetGuest(arguments[0]);
                    break;

                case @"set-setting":
                    Configuration.OsuSetting key = System.Enum.Parse<Configuration.OsuSetting>(arguments[0]);
                    string value = arguments[1];
                    if (value == @"true" || value == @"false")
                        game.SetSetting(key, value == @"true");
                    else if (int.TryParse(value, out int i))
                        game.SetSetting(key, i);
                    else if (double.TryParse(value, out double d))
                        game.SetSetting(key, d);
                    else if (value.StartsWith('"') && value.EndsWith('"'))
                        game.SetSetting(key, Newtonsoft.Json.JsonConvert.DeserializeObject<string>(value));
                    else
                        throw new System.ArgumentException(@"Unrecognized value type for set-setting (expected JSON value)");
                    break;
            }
        }
    }

    public class CommandMessage
    {
        public string Command;
        public string[] Arguments;
    }
}
