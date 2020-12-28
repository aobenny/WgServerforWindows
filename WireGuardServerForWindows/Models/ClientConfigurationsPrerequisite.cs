﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ClientConfigurationsPrerequisite : PrerequisiteItem
    {
        public ClientConfigurationsPrerequisite() : base
        (
            title: Resources.ClientConfigurations,
            successMessage: Resources.ClientConfigurationsSuccessMessage,
            errorMessage: Resources.ClientConfigurationsMissingErrorMessage,
            resolveText: Resources.ClientConfigurationsResolveText,
            configureText: Resources.ClientConfigurationsResolveText
        ) { }

        public override bool Fulfilled
        {
            get
            {
                bool result = true;

                if (Directory.Exists(ClientConfigurationDirectory) == false || Directory.GetFiles(ClientConfigurationDirectory).Any() == false)
                {
                    result = false;
                    ErrorMessage = Resources.ClientConfigurationsMissingErrorMessage;
                }
                else
                {
                    // Validate all of the client(s)
                    foreach (string clientConfigurationFile in Directory.GetFiles(ClientConfigurationDirectory, "*.conf"))
                    {
                        var clientConfiguration = new ClientConfiguration(null).Load(clientConfigurationFile);

                        foreach (ConfigurationProperty property in clientConfiguration.Properties)
                        {
                            if (string.IsNullOrEmpty(property.Validation?.Validate?.Invoke(property)) == false)
                            {
                                result = false;
                                ErrorMessage = Resources.ClientConfigurationsIncompleteErrorMessage;
                                goto finish;
                            }
                        }
                    }
                }

                finish:
                return result;
            }
        }

        public override void Resolve()
        {
            if (Directory.Exists(ClientConfigurationDirectory) == false)
            {
                Directory.CreateDirectory(ClientConfigurationDirectory);
            }

            Configure();
        }

        public override void Configure()
        {
            ClientConfigurationList clientConfigurations = new ClientConfigurationList();

            foreach (string clientConfigurationFile in Directory.GetFiles(ClientConfigurationDirectory, "*.conf"))
            {
                clientConfigurations.List.Add(new ClientConfiguration(clientConfigurations).Load<ClientConfiguration>(clientConfigurationFile));
            }

            ClientConfigurationEditorWindow clientConfigurationEditorWindow = new ClientConfigurationEditorWindow {DataContext = clientConfigurations};

            Mouse.OverrideCursor = Cursors.Wait;
            if (clientConfigurationEditorWindow.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                foreach (ClientConfiguration clientConfiguration in clientConfigurations.List)
                {
                    clientConfiguration.Save(Path.Combine(ClientConfigurationDirectory, $"{clientConfiguration.NameProperty.Value}.conf"));
                }
                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        #region Public properties

        public string ClientConfigurationDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "clients");

        #endregion
    }
}