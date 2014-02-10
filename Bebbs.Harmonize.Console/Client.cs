﻿using System.Linq;
using System;
using Bebbs.Harmonize.With;
using System.Collections.Generic;

namespace Bebbs.Harmonize.Console
{
    public class Client
    {
        private readonly Options _options;
        private Harmonizer _harmonizer;

        public Client(Options options)
        {
            _options = options;
        }

        public async void Start()
        {
            List<HarmonizedModule> modules = new List<HarmonizedModule>();
            modules.Add(new Harmony.Module());

            if (_options.UseAllJoyn)
            {
                modules.Add(new Harmonize.With.Alljoyn.Module());
            }

            if (_options.UseMessaging)
            {
                modules.Add(new Harmonize.With.Messaging.Module());
            }

            Harmonize.Options harmonizeOptions = new Harmonize.Options(modules);

            _harmonizer = new Harmonize.Harmonizer(harmonizeOptions);

            await _harmonizer.Start(new Settings.Provider());

            /* This code powers on the amp once harmonize has started, kept here for reference now but will be moved shortly
            var device = result.Devices.Where(d => string.Equals(d.Type, "Amplifier", StringComparison.CurrentCultureIgnoreCase) && string.Equals(d.Model, "DSP-A5", StringComparison.CurrentCultureIgnoreCase)).First();
            var control = device.Controls.Where(c => string.Equals(c.Name, "Power", StringComparison.CurrentCultureIgnoreCase)).First();
            var action = control.Actions.Where(a => string.Equals(a.Name, "PowerOn", StringComparison.CurrentCultureIgnoreCase)).First();
            var command = action.Command;

            _harmonizer.SendCommand(command);
            */
        }

        public async void Stop()
        {
            if (_harmonizer != null)
            {
                await _harmonizer.Stop();

                _harmonizer = null;
            }
        }
    }
}
