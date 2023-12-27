using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using SMGI_Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMGI_Plugin_EmergencyMap
{
    internal class SMGI应急快速制图 : Module, IExtensionConfig
    {
        private static SMGI应急快速制图 _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static SMGI应急快速制图 Current => _this ??= (SMGI应急快速制图)FrameworkApplication.FindModule("SMGI_Plugin_EmergencyMap_Module");

        #region 产品注册相关

        private static string _authorizationID = "";
        private static ExtensionState _extensionState;

        internal static string AuthorizationID
        {
            get
            {
                return _authorizationID;
            }
            set
            {
                _authorizationID = value;
            }
        }

        /// <summary>
        /// Implement to override the extensionConfig in the DAML
        /// </summary>
        public string Message
        {
            get { return ""; }
            set { }
        }

        /// <summary>
        /// Implement to override the extensionConfig in the DAML
        /// </summary>
        public string ProductName
        {
            get { return ""; }
            set { }
        }

        /// <summary>
        /// Handle enable/disable request from the UI
        /// </summary>
        public ExtensionState State
        {
            get
            {
                return _extensionState;
            }
            set
            {
                if (value == ExtensionState.Unavailable)
                {
                    return; //Leave the state Unavailable
                }
                else if (value == ExtensionState.Disabled)
                {
                    FrameworkApplication.State.Deactivate("SMGI_module_licensed");
                    _extensionState = value;

                }
                else
                {
                    if (GApplication.CheckRegistration(ProductAttribute.Product))
                    {
                        FrameworkApplication.State.Activate("SMGI_module_licensed");
                        _extensionState = ExtensionState.Enabled;
                    }
                    else
                    {
                        RegFrom reg = new RegFrom();
                        reg.ShowDialog();

                        if (GApplication.CheckRegistration(ProductAttribute.Product))
                        {
                            FrameworkApplication.State.Activate("SMGI_module_licensed");
                            _extensionState = ExtensionState.Enabled;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Execute your authorization check
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static void CheckLicensing()
        {
            if (GApplication.CheckRegistration(ProductAttribute.Product))
            {
                FrameworkApplication.State.Activate("SMGI_module_licensed");
                _extensionState = ExtensionState.Enabled;
            }
            else
            {
                FrameworkApplication.State.Deactivate("SMGI_module_licensed");
                _extensionState = ExtensionState.Disabled;
            }
        }

        private SMGI应急快速制图()
        {

            //TODO - read authorization id from....
            //file, url, etc. as required

            //preset _authorizationID to a number "string" divisible by 2 to have 
            //the Add-in initially enabled
            CheckLicensing();
        }

        #endregion


        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        protected override bool Initialize()
        {
            QueuedTask.Run(() =>
            {
                GApplication.InitializeLog(GApplication.Application.ResourcePath + @"\Resources\Log" + "\\log4net.config");

                Helper.CreateTempWorkspace(GApplication.Application.AppDataPath, "MyWorkspace.gdb");
            });

            return base.Initialize();
        }

        #endregion Overrides

    }
}
