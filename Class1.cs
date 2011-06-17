using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using MediaPortal.GUI.Library;
using MediaPortal.InputDevices;
using MediaPortal.Configuration;
using REMOTESERVICELib;
using System.IO;

namespace MediaPortal.Plugins
{

    /// <summary>
    /// AverMedia RM-HV remote plugin (with service AverRemote.exe 1.0.1.4)
    /// </summary>
    [PluginIcons("AverRMHV.iconoon.jpg", "AverRMHV.iconooff.jpg")]
    public class AverRemote : ISetupForm, IPlugin
    {


        #region Propios

        const string mappingfile = "AverRmHv";
        private RemoteClass rc;
        private InputHandler inputhandler;
        private double interKeyDelay = 200;       // inter-key delay in ms
        private DateTime lastTimeActionPerformed; // records the time an action was performed
        private DateTime lastTimeButtonPushed;    // records the time a button was pushed
        private uint lastnKeyFunPressed = 0x00000000;
        private int sameButtonPushedCount = 0;    // tracks the number of times the same button was pushed in a row

        /// <summary>
        /// Callback Function
        /// </summary>
        public void RecibeDatos(uint nKeyFun, uint nKey, uint dwKeyCode)
        {
            TimeSpan timeSinceActionTaken;    // timespan since the last action was performed
            TimeSpan timeSinceButtonPushed;   // timespan since the last button was pushed
            double timeSinceActionTakenInMS;  // timeSinceActionTaken in milliseconds
            double timeSinceButtonPushedInMS; // timeSinceButtonPushed in milliseconds
            DateTime now = DateTime.Now;      // the time now

            //File.AppendAllText(@"c:\AverRmHv.txt", "Datos Recibidos: " + nKey.ToString() + " " + nKeyFun.ToString() +"\n\r");
            //ignore message: 0x000004ca. Messages come in pairs. This code is 1226
            //if (nKey == 0x000004c9 && inputhandler != null && inputhandler.IsLoaded)
            if (nKey != 0x000004ca && inputhandler != null && inputhandler.IsLoaded)
            {
                // calculate the time passed since a button was pressed
                timeSinceButtonPushed = now - lastTimeButtonPushed;
                timeSinceButtonPushedInMS = timeSinceButtonPushed.TotalMilliseconds;

                // calculate the time since an action was performed
                timeSinceActionTaken = now - lastTimeActionPerformed;
                timeSinceActionTakenInMS = timeSinceActionTaken.TotalMilliseconds;

                // increase the counter for the number of times the same key has been pressed
                //if (nKeyFun == lastnKeyFunPressed && (totalKeyPressedTimeSpan == 0 || keyPressedTimeSpan == 156 || totalKeyPressedTimeSpan == 187.5 || totalKeyPressedTimeSpan == 203.125)) // keyPressedTimeSpan == 140 
                if (nKeyFun == lastnKeyFunPressed)
                { 
                    sameButtonPushedCount++; 
                }
                else 
                { 
                    sameButtonPushedCount = 0; 
                }

                // nKeyFun.ToString() - codes for keys
                // up       = 15
                // down     = 16
                // right    = 17
                // left     = 18
                // channel+ = 29
                // channel- = 30
                Boolean navigationKeyPressed = (
                    nKeyFun == 15 || 
                    nKeyFun == 16 ||
                    nKeyFun == 17 ||
                    nKeyFun == 18 ||
                    nKeyFun == 29 ||
                    nKeyFun == 30
                );

                // log some useful internal information - for debugging purposes
                Log.Debug("AverRMHV: " + 
                    "button pressed count: "     + sameButtonPushedCount     + ": " + 
                    "time since action taken: "  + timeSinceButtonPushedInMS + "ms, " +
                    "time since button pushed: " + timeSinceActionTakenInMS  + "ms, " +
                    "Data Received: " + 
                    nKey.ToString() + " " + nKeyFun.ToString() + " " + navigationKeyPressed
                    );

                // perform the action 
                // if (now - last_time_action_performed) > timeout_period, then perform action
                if (timeSinceActionTakenInMS > (interKeyDelay+10))
                {
                    Log.Debug("AverRMHV:   KEY ACTIONED: " + nKeyFun.ToString());
                    inputhandler.MapAction((int)nKeyFun);
                    lastTimeActionPerformed = DateTime.Now;
                }
                else
                {
                    Log.Debug("AverRMHV:   KEY IGNORED: " + nKeyFun.ToString());
                }
                lastTimeButtonPushed = DateTime.Now;
                lastnKeyFunPressed = nKeyFun;
            }
        }

        /// <summary>
        /// Constructor (nothing to do)
        /// </summary>
        /* public AverRemote()
         {
             //File.AppendAllText(@"c:\AverRmHv.txt", "Constructor Llamado\n\r");
         }*/

        #endregion Propios

        #region IPlugin Members

        /// <summary>
        /// Starts this instance.
        /// </summary>
        void IPlugin.Start()
        {

            //File.AppendAllText(@"c:\AverRmHv.txt", "Start Llamado\n\r");

            try
            {
                rc = new RemoteClass();
                rc.Initialize();
                rc.OnRemoteData += RecibeDatos;
                //string mediaportalpath = AppDomain.CurrentDomain.ApplicationIdentity.FullName;
                string mediaportalpath = System.Reflection.Assembly.GetEntryAssembly().FullName;
                rc.SwitchBeginAP(mediaportalpath);
                Log.Info("AverRMHV Plugin: Started by " + mediaportalpath);
                //File.AppendAllText(@"c:\AverRmHv.txt", "Inicialización Correcta. Path = " + mediaportalpath);


            }
            catch (Exception e)
            {
                //File.AppendAllText(@"c:\AverRmHv.txt", "Error en Start\n\r");
                Log.Error("AverRMHV Plugin: AverRemote.exe not responding");
                Log.Error("AverRMHV Plugin: Exception: "+e);
            }

            inputhandler = new InputHandler(mappingfile);
            if (inputhandler == null || !inputhandler.IsLoaded)
            {
                //File.AppendAllText(@"c:\AverRmHv.txt", "AverRMHV Plugin: File " + mappingfile + " not loaded.\n\r");
                Log.Error("AverRMHV Plugin: File " + mappingfile + " not loaded.");
            }
            lastTimeActionPerformed = DateTime.Now;
            lastTimeButtonPushed = DateTime.Now;
            Log.Info("AverRMHV Plugin: Started.");
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void IPlugin.Stop()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "Stop Llamado\n\r");

            try
            {
                if (rc != null)
                {
                    rc.OnRemoteData -= RecibeDatos;
                    rc.Uninitialize();
                }
            }
            catch { }
            Log.Info("AverRMHV Plugin: Stopped.");
        }

        #endregion IPlugin Members

        #region ISetupForm Members

        string ISetupForm.Author()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "Author Llamado\n\r");
            return "Pantav";
        }
        bool ISetupForm.CanEnable()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "CanEnable Llamado\n\r");
            return true;
        }
        bool ISetupForm.DefaultEnabled()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "DefaultEnabled Llamado\n\r");
            return true;
        }
        string ISetupForm.Description()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "Description Llamado\n\r");
            return "Plugin for Aver RM-HV remote included with some cards";
        }

        int ISetupForm.GetWindowId()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "GetWindowId Llamado\n\r");
            return -1; //It's a process plugin
        }

        bool ISetupForm.HasSetup()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "HasSetup Llamado\n\r");
            return true;
        }
        string ISetupForm.PluginName()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "PluginName Llamado\n\r");
            return "AverRMHVremote";
        }

        void ISetupForm.ShowPlugin()
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "ShowPlugin Llamado\n\r");
            InputMappingForm conf = new InputMappingForm(mappingfile);
            conf.ShowDialog();
        }

        bool ISetupForm.GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            //File.AppendAllText(@"c:\AverRmHv.txt", "GetHome Llamado\n\r");
            strButtonText = String.Empty;
            strButtonImage = String.Empty;
            strButtonImageFocus = String.Empty;
            strPictureImage = String.Empty;
            return false;
        }

        #endregion ISetupForm Members


    }
}