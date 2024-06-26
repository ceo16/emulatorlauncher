﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LumacaUpdater;
using EmulatorLauncher.Common;

namespace es_checkversion
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                string branch = args.SkipWhile(a => a != "-branch").Skip(1).FirstOrDefault();
                if (string.IsNullOrEmpty(branch))
                    branch = "stable";

                string localVersion = LumacaVersion.GetLocalVersion();
                if (string.IsNullOrEmpty(localVersion))
                    throw new ApplicationException("Lumaca is not properly installed");

                string remoteVersion = LumacaVersion.GetRemoteVersion(branch);
                if (string.IsNullOrEmpty(remoteVersion))
                    throw new ApplicationException("Unable to get remote version");

                if (localVersion != remoteVersion)
                {
                    Console.WriteLine(remoteVersion);
                    return 0;
                }

                return 1;
            }
            catch(Exception ex)
            {
                SimpleLogger.Instance.Error(ex.Message);
                return 1;
            }
        }
    }
}
