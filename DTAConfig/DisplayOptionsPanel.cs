﻿using ClientCore;
using ClientGUI;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace DTAConfig
{
    class DisplayOptionsPanel : XNAOptionsPanel
    {
        private const int DRAG_DISTANCE_DEFAULT = 4;
        private const int ORIGINAL_RESOLUTION_WIDTH = 640;
        private const string RENDERERS_INI = "Renderers.ini";

        public DisplayOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        private XNAClientDropDown ddIngameResolution;
        private XNAClientDropDown ddDetailLevel;
        private XNAClientDropDown ddRenderer;
        private XNAClientCheckBox chkWindowedMode;
        private XNAClientCheckBox chkBorderlessWindowedMode;
        private XNAClientCheckBox chkBackBufferInVRAM;
        private XNAClientDropDown ddClientResolution;
        private XNAClientCheckBox chkBorderlessClient;
        private XNAClientDropDown ddClientTheme;

        private List<FileSettingCheckBox> fileSettingCheckBoxes = new List<FileSettingCheckBox>();
        private List<DirectDrawWrapper> renderers;

        private string defaultRenderer;

#if !YR
        private XNALabel lblCompatibilityFixes;
        private XNALabel lblGameCompatibilityFix;
        private XNALabel lblMapEditorCompatibilityFix;
        private XNAClientButton btnGameCompatibilityFix;
        private XNAClientButton btnMapEditorCompatibilityFix;

        private bool GameCompatFixInstalled = false;
        private bool FinalSunCompatFixInstalled = false;
        private bool GameCompatFixDeclined = false;
        //private bool FinalSunCompatFixDeclined = false;
#endif

#if DTA
        private FileSettingCheckBox chkEnableCannonTracers;
#elif TI
        private FileSettingCheckBox chkLargerInfantryGraphics;
        private FileSettingCheckBox chkSmallerVehicleGraphics;
#endif



        public override void Initialize()
        {
            base.Initialize();

            Name = "DisplayOptionsPanel";

            var lblIngameResolution = new XNALabel(WindowManager);
            lblIngameResolution.Name = "lblIngameResolution";
            lblIngameResolution.ClientRectangle = new Rectangle(12, 14, 0, 0);
            lblIngameResolution.Text = "In-game Resolution:";

            ddIngameResolution = new XNAClientDropDown(WindowManager);
            ddIngameResolution.Name = "ddIngameResolution";
            ddIngameResolution.ClientRectangle = new Rectangle(
                lblIngameResolution.ClientRectangle.Right + 12,
                lblIngameResolution.ClientRectangle.Y - 2, 120, 19);

            var clientConfig = ClientConfiguration.Instance;

            var resolutions = GetResolutions(clientConfig.MinimumIngameWidth, 
                clientConfig.MinimumIngameHeight,
                clientConfig.MaximumIngameWidth, clientConfig.MaximumIngameHeight);

            resolutions.Sort();

            foreach (var res in resolutions)
                ddIngameResolution.AddItem(res.ToString());

            var  lblDetailLevel = new XNALabel(WindowManager);
            lblDetailLevel.Name = "lblDetailLevel";
            lblDetailLevel.ClientRectangle = new Rectangle(lblIngameResolution.ClientRectangle.X,
                ddIngameResolution.ClientRectangle.Bottom + 16, 0, 0);
            lblDetailLevel.Text = "Detail Level:";

            ddDetailLevel = new XNAClientDropDown(WindowManager);
            ddDetailLevel.Name = "ddDetailLevel";
            ddDetailLevel.ClientRectangle = new Rectangle(
                ddIngameResolution.ClientRectangle.X,
                lblDetailLevel.ClientRectangle.Y - 2,
                ddIngameResolution.ClientRectangle.Width, 
                ddIngameResolution.ClientRectangle.Height);
            ddDetailLevel.AddItem("Low");
            ddDetailLevel.AddItem("Medium");
            ddDetailLevel.AddItem("High");

            var  lblRenderer = new XNALabel(WindowManager);
            lblRenderer.Name = "lblRenderer";
            lblRenderer.ClientRectangle = new Rectangle(lblDetailLevel.ClientRectangle.X,
                ddDetailLevel.ClientRectangle.Bottom + 16, 0, 0);
            lblRenderer.Text = "Renderer:";

            ddRenderer = new XNAClientDropDown(WindowManager);
            ddRenderer.Name = "ddRenderer";
            ddRenderer.ClientRectangle = new Rectangle(
                ddDetailLevel.ClientRectangle.X,
                lblRenderer.ClientRectangle.Y - 2,
                ddDetailLevel.ClientRectangle.Width,
                ddDetailLevel.ClientRectangle.Height);

            GetRenderers();

            var localOS = ClientConfiguration.Instance.GetOperatingSystemVersion();

            foreach (var renderer in renderers)
            {
                if (renderer.IsCompatibleWithOS(localOS))
                {
                    ddRenderer.AddItem(new XNADropDownItem()
                    {
                        Text = renderer.UIName,
                        TextColor = UISettings.AltColor,
                        Tag = renderer
                    });
                }
            }

            //ddRenderer.AddItem("Default");
            //ddRenderer.AddItem("IE-DDRAW");
            //ddRenderer.AddItem("TS-DDRAW");
            //ddRenderer.AddItem("DDWrapper");
            //ddRenderer.AddItem("DxWnd");
            //if (ClientConfiguration.Instance.GetOperatingSystemVersion() == OSVersion.WINXP)
            //    ddRenderer.AddItem("Software");

            chkWindowedMode = new XNAClientCheckBox(WindowManager);
            chkWindowedMode.Name = "chkWindowedMode";
            chkWindowedMode.ClientRectangle = new Rectangle(lblDetailLevel.ClientRectangle.X,
                ddRenderer.ClientRectangle.Bottom + 16, 0, 0);
            chkWindowedMode.Text = "Windowed Mode";
            chkWindowedMode.CheckedChanged += ChkWindowedMode_CheckedChanged;

            chkBorderlessWindowedMode = new XNAClientCheckBox(WindowManager);
            chkBorderlessWindowedMode.Name = "chkBorderlessWindowedMode";
            chkBorderlessWindowedMode.ClientRectangle = new Rectangle(
                chkWindowedMode.ClientRectangle.X + 50,
                chkWindowedMode.ClientRectangle.Bottom + 24, 0, 0);
            chkBorderlessWindowedMode.Text = "Borderless Windowed Mode";
            chkBorderlessWindowedMode.AllowChecking = false;

            chkBackBufferInVRAM = new XNAClientCheckBox(WindowManager);
            chkBackBufferInVRAM.Name = "chkBackBufferInVRAM";
            chkBackBufferInVRAM.ClientRectangle = new Rectangle(
                lblDetailLevel.ClientRectangle.X,
                chkBorderlessWindowedMode.ClientRectangle.Bottom + 28, 0, 0);
            chkBackBufferInVRAM.Text = "Back Buffer in Video Memory" + Environment.NewLine +
                "(lower performance, but is" + Environment.NewLine + "necessary on some systems)";

            var  lblClientResolution = new XNALabel(WindowManager);
            lblClientResolution.Name = "lblClientResolution";
            lblClientResolution.ClientRectangle = new Rectangle(
                285, 14, 0, 0);
            lblClientResolution.Text = "Client Resolution:";

            ddClientResolution = new XNAClientDropDown(WindowManager);
            ddClientResolution.Name = "ddClientResolution";
            ddClientResolution.ClientRectangle = new Rectangle(
                lblClientResolution.ClientRectangle.Right + 12,
                lblClientResolution.ClientRectangle.Y - 2,
                ClientRectangle.Width - (lblClientResolution.ClientRectangle.Right + 24),
                ddIngameResolution.ClientRectangle.Height);
            ddClientResolution.AllowDropDown = false;

            var screenBounds = Screen.PrimaryScreen.Bounds;

            resolutions = GetResolutions(800, 600,
                screenBounds.Width, screenBounds.Height);

            // Add "optimal" client resolutions for windowed mode
            // if they're not supported in fullscreen mode

            AddResolutionIfFitting(1024, 600, resolutions);
            AddResolutionIfFitting(1024, 720, resolutions);
            AddResolutionIfFitting(1280, 600, resolutions);
            AddResolutionIfFitting(1280, 720, resolutions);
            AddResolutionIfFitting(1280, 768, resolutions);
            AddResolutionIfFitting(1280, 800, resolutions);

            resolutions.Sort();

            foreach (var res in resolutions)
            {
                var item = new XNADropDownItem();
                item.Text = res.ToString();
                item.Tag = res.ToString();
                item.TextColor = UISettings.AltColor;
                ddClientResolution.AddItem(item);
            }

            // So we add the optimal resolutions to the list, sort it and then find
            // out the optimal resolution index - it's inefficient, but works

            int optimalWindowedResIndex = resolutions.FindIndex(res => res.ToString() == "1280x800");
            if (optimalWindowedResIndex == -1)
                optimalWindowedResIndex = resolutions.FindIndex(res => res.ToString() == "1280x768");

            if (optimalWindowedResIndex > -1)
            {
                var item = ddClientResolution.Items[optimalWindowedResIndex];
                item.Text = item.Text + " " + ClientConfiguration.Instance.ClientDefaultResolutionText;
            }

            chkBorderlessClient = new XNAClientCheckBox(WindowManager);
            chkBorderlessClient.Name = "chkBorderlessClient";
            chkBorderlessClient.ClientRectangle = new Rectangle(
                lblClientResolution.ClientRectangle.X,
                lblDetailLevel.ClientRectangle.Y, 0, 0);
            chkBorderlessClient.Text = "Fullscreen Client";
            chkBorderlessClient.CheckedChanged += ChkBorderlessMenu_CheckedChanged;
            chkBorderlessClient.Checked = true;

            var lblClientTheme = new XNALabel(WindowManager);
            lblClientTheme.Name = "lblClientTheme";
            lblClientTheme.ClientRectangle = new Rectangle(
                lblClientResolution.ClientRectangle.X,
                lblRenderer.ClientRectangle.Y, 0, 0);
            lblClientTheme.Text = "Client Theme:";

            ddClientTheme = new XNAClientDropDown(WindowManager);
            ddClientTheme.Name = "ddClientTheme";
            ddClientTheme.ClientRectangle = new Rectangle(
                ddClientResolution.ClientRectangle.X,
                ddRenderer.ClientRectangle.Y,
                ddClientResolution.ClientRectangle.Width,
                ddRenderer.ClientRectangle.Height);

            int themeCount = ClientConfiguration.Instance.ThemeCount;

            for (int i = 0; i < themeCount; i++)
                ddClientTheme.AddItem(ClientConfiguration.Instance.GetThemeInfoFromIndex(i)[0]);

#if DTA
            chkEnableCannonTracers = new FileSettingCheckBox(WindowManager,
                "Resources\\ECache91.mix", "MIX\\ECache91.mix", true);
            chkEnableCannonTracers.Name = "chkEnableCannonTracers";
            chkEnableCannonTracers.ClientRectangle = new Rectangle(
                chkBorderlessClient.ClientRectangle.X,
                chkWindowedMode.ClientRectangle.Y, 0, 0);
            chkEnableCannonTracers.Text = "Use Cannon Tracers";

            AddChild(chkEnableCannonTracers);

            fileSettingCheckBoxes.Add(chkEnableCannonTracers);
#elif TI
            chkSmallerVehicleGraphics = new FileSettingCheckBox(WindowManager,
                "Resources\\ecache02.mix", "MIX\\ecache02.mix", false);
            chkSmallerVehicleGraphics.AddFile("Resources\\expand02.mix", "MIX\\expand02.mix");
            chkSmallerVehicleGraphics.Name = "chkSmallerVehicleGraphics";
            chkSmallerVehicleGraphics.ClientRectangle = new Rectangle(
                chkBorderlessClient.ClientRectangle.X,
                chkWindowedMode.ClientRectangle.Y, 0, 0);
            chkSmallerVehicleGraphics.Text = "Smaller Vehicle Graphics";

            AddChild(chkSmallerVehicleGraphics);
            fileSettingCheckBoxes.Add(chkSmallerVehicleGraphics);

            chkLargerInfantryGraphics = new FileSettingCheckBox(WindowManager,
                "Resources\\ecache01.mix", "MIX\\ecache01.mix", false);
            chkLargerInfantryGraphics.Name = "chkLargerInfantryGraphics";
            chkLargerInfantryGraphics.ClientRectangle = new Rectangle(
                chkSmallerVehicleGraphics.ClientRectangle.X,
                chkBorderlessWindowedMode.ClientRectangle.Y, 0, 0);
            chkLargerInfantryGraphics.Text = "Larger Infantry Graphics";

            AddChild(chkLargerInfantryGraphics);
            fileSettingCheckBoxes.Add(chkLargerInfantryGraphics);
#endif

#if !YR
            lblCompatibilityFixes = new XNALabel(WindowManager);
            lblCompatibilityFixes.Name = "lblCompatibilityFixes";
            lblCompatibilityFixes.FontIndex = 1;
            lblCompatibilityFixes.Text = "Compatibility Fixes (advanced):";
            AddChild(lblCompatibilityFixes);
            lblCompatibilityFixes.CenterOnParent();
            lblCompatibilityFixes.ClientRectangle = new Rectangle(
                lblCompatibilityFixes.ClientRectangle.X,
                ClientRectangle.Height - 103,
                lblCompatibilityFixes.ClientRectangle.Width,
                lblCompatibilityFixes.ClientRectangle.Height);

            lblGameCompatibilityFix = new XNALabel(WindowManager);
            lblGameCompatibilityFix.Name = "lblGameCompatibilityFix";
            lblGameCompatibilityFix.ClientRectangle = new Rectangle(132, 
                lblCompatibilityFixes.ClientRectangle.Bottom + 20, 0, 0);
            lblGameCompatibilityFix.Text = "DTA/TI/TS Compatibility Fix:";

            btnGameCompatibilityFix = new XNAClientButton(WindowManager);
            btnGameCompatibilityFix.Name = "btnGameCompatibilityFix";
            btnGameCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.ClientRectangle.Right + 20,
                lblGameCompatibilityFix.ClientRectangle.Y - 4, 133, 23);
            btnGameCompatibilityFix.FontIndex = 1;
            btnGameCompatibilityFix.Text = "Enable";
            btnGameCompatibilityFix.LeftClick += BtnGameCompatibilityFix_LeftClick;

            lblMapEditorCompatibilityFix = new XNALabel(WindowManager);
            lblMapEditorCompatibilityFix.Name = "lblMapEditorCompatibilityFix";
            lblMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                lblGameCompatibilityFix.ClientRectangle.X,
                lblGameCompatibilityFix.ClientRectangle.Bottom + 20, 0, 0);
            lblMapEditorCompatibilityFix.Text = "FinalSun Compatibility Fix:";

            btnMapEditorCompatibilityFix = new XNAClientButton(WindowManager);
            btnMapEditorCompatibilityFix.Name = "btnMapEditorCompatibilityFix";
            btnMapEditorCompatibilityFix.ClientRectangle = new Rectangle(
                btnGameCompatibilityFix.ClientRectangle.X,
                lblMapEditorCompatibilityFix.ClientRectangle.Y - 4,
                btnGameCompatibilityFix.ClientRectangle.Width,
                btnGameCompatibilityFix.ClientRectangle.Height);
            btnMapEditorCompatibilityFix.FontIndex = 1;
            btnMapEditorCompatibilityFix.Text = "Enable";
            btnMapEditorCompatibilityFix.LeftClick += BtnMapEditorCompatibilityFix_LeftClick;

            AddChild(lblGameCompatibilityFix);
            AddChild(btnGameCompatibilityFix);
            AddChild(lblMapEditorCompatibilityFix);
            AddChild(btnMapEditorCompatibilityFix);
#endif

            AddChild(chkWindowedMode);
            AddChild(chkBorderlessWindowedMode);
            AddChild(chkBackBufferInVRAM);
            AddChild(chkBorderlessClient);
            AddChild(lblClientTheme);
            AddChild(ddClientTheme);
            AddChild(lblClientResolution);
            AddChild(ddClientResolution);
            AddChild(lblRenderer);
            AddChild(ddRenderer);
            AddChild(lblDetailLevel);
            AddChild(ddDetailLevel);
            AddChild(lblIngameResolution);
            AddChild(ddIngameResolution);
        }

        /// <summary>
        /// Adds a screen resolution to a list of resolutions if it fits on the screen.
        /// Checks if the resolution already exists before adding it.
        /// </summary>
        /// <param name="width">The width of the new resolution.</param>
        /// <param name="height">The height of the new resolution.</param>
        /// <param name="resolutions">A list of screen resolutions.</param>
        private void AddResolutionIfFitting(int width, int height, List<ScreenResolution> resolutions)
        {
            if (resolutions.Find(res => res.Width == width && res.Height == height) != null)
                return;

            var screenBounds = Screen.PrimaryScreen.Bounds;

            if (screenBounds.Width >= width && screenBounds.Height >= height)
            {
                resolutions.Add(new ScreenResolution(width, height));
            }
        }

        private void GetRenderers()
        {
            renderers = new List<DirectDrawWrapper>();

            var renderersIni = new IniFile(ProgramConstants.GetBaseResourcePath() + RENDERERS_INI);

            var keys = renderersIni.GetSectionKeys("Renderers");
            if (keys == null)
                throw new Exception("[Renderers] not found from Renderers.ini!");

            foreach (string key in keys)
            {
                string internalName = renderersIni.GetStringValue("Renderers", key, string.Empty);

                var ddWrapper = new DirectDrawWrapper(internalName, renderersIni);
                renderers.Add(ddWrapper);
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            defaultRenderer = renderersIni.GetStringValue("DefaultRenderer", osVersion.ToString(), string.Empty);

            if (defaultRenderer == null)
                throw new Exception("Invalid or missing default renderer for operating system: " + osVersion);
        }

#if !YR

        /// <summary>
        /// Asks the user whether they want to install the DTA/TI/TS compatibility fix.
        /// </summary>
        public void PostInit()
        {
            Load();

            if (!GameCompatFixInstalled && !GameCompatFixDeclined)
            {
                string defaultGame = ClientConfiguration.Instance.LocalGame;

                var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "New Compatibility Fix",
                    "A performance-enhancing compatibility fix for modern Windows versions" + Environment.NewLine +
                    "has been included in this version of " + defaultGame + ". Enabling it requires" + Environment.NewLine +
                    "administrative priveleges. Would you like to install the compatibility fix?" + Environment.NewLine + Environment.NewLine + 
                    "You'll always be able to install or uninstall the compatibility fix later from the options menu.");
                messageBox.YesClickedAction = MessageBox_YesClicked;
                messageBox.NoClickedAction = MessageBox_NoClicked;
            }
        }

        private void MessageBox_NoClicked(XNAMessageBox messageBox)
        {
            // Set compatibility fix declined flag in registry
            try
            {
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client");

                try
                {
                    regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("TSCompatFixDeclined", "Yes");
                }
                catch (Exception ex)
                {
                    Logger.Log("Setting TSCompatFixDeclined failed! Returned error: " + ex.Message);
                }
            }
            catch { }
        }

        private void MessageBox_YesClicked(XNAMessageBox messageBox)
        {
            BtnGameCompatibilityFix_LeftClick(messageBox, EventArgs.Empty);
        }

        private void BtnGameCompatibilityFix_LeftClick(object sender, EventArgs e)
        {
            if (GameCompatFixInstalled)
            {
                try
                {
                    Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"TS Compatibility Fix\"");

                    sdbinst.WaitForExit();

                    Logger.Log("DTA/TI/TS Compatibility Fix succesfully uninstalled.");
                    XNAMessageBox.Show(WindowManager, "Compatibility Fix Uninstalled",
                        "The DTA/TI/TS Compatibility Fix has been succesfully uninstalled.");

                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("TSCompatFixInstalled", "No");

                    btnGameCompatibilityFix.Text = "Enable";

                    GameCompatFixInstalled = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling DTA/TI/TS Compatibility Fix failed. Error message: " + ex.Message);
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed",
                        "Uninstalling DTA/TI/TS Compatibility Fix failed. Returned error: " + ex.Message);
                }

                return;
            }

            try
            {
                Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources\\compatfix.sdb\"");

                sdbinst.WaitForExit();

                Logger.Log("DTA/TI/TS Compatibility Fix succesfully installed.");
                XNAMessageBox.Show(WindowManager, "Compatibility Fix Installed",
                    "The DTA/TI/TS Compatibility Fix has been succesfully installed.");

                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("TSCompatFixInstalled", "Yes");

                btnGameCompatibilityFix.Text = "Disable";

                GameCompatFixInstalled = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Installing DTA/TI/TS Compatibility Fix failed. Error message: " + ex.Message);
                XNAMessageBox.Show(WindowManager, "Installing Compatibility Fix Failed",
                    "Installing DTA/TI/TS Compatibility Fix failed. Error message: " + ex.Message);
            }
        }

        private void BtnMapEditorCompatibilityFix_LeftClick(object sender, EventArgs e)
        {
            if (FinalSunCompatFixInstalled)
            {
                try
                {
                    Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"Final Sun Compatibility Fix\"");

                    sdbinst.WaitForExit();

                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("FSCompatFixInstalled", "No");

                    btnMapEditorCompatibilityFix.Text = "Enable";

                    Logger.Log("FinalSun Compatibility Fix succesfully uninstalled.");
                    XNAMessageBox.Show(WindowManager, "Compatibility Fix Uninstalled",
                        "The FinalSun Compatibility Fix has been succesfully uninstalled.");

                    FinalSunCompatFixInstalled = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.Message);
                    XNAMessageBox.Show(WindowManager, "Uninstalling Compatibility Fix Failed",
                        "Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.Message);
                }

                return;
            }


            try
            {
                Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources\\FSCompatFix.sdb\"");

                sdbinst.WaitForExit();

                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("FSCompatFixInstalled", "Yes");

                btnMapEditorCompatibilityFix.Text = "Disable";

                Logger.Log("FinalSun Compatibility Fix succesfully installed.");
                XNAMessageBox.Show(WindowManager, "Compatibility Fix Installed",
                    "The FinalSun Compatibility Fix has been succesfully installed.");

                FinalSunCompatFixInstalled = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Installing FinalSun Compatibility Fix failed. Error message: " + ex.Message);
                XNAMessageBox.Show(WindowManager, "Installing Compatibility Fix Failed",
                    "Installing FinalSun Compatibility Fix failed. Error message: " + ex.Message);
            }
        }

#endif

        private void ChkBorderlessMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBorderlessClient.Checked)
            {
                ddClientResolution.AllowDropDown = false;
                string nativeRes = Screen.PrimaryScreen.Bounds.Width +
                    "x" + Screen.PrimaryScreen.Bounds.Height;

                int nativeResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == nativeRes);
                if (nativeResIndex > -1)
                    ddClientResolution.SelectedIndex = nativeResIndex;
            }
            else
            {
                ddClientResolution.AllowDropDown = true;

                int optimalWindowedResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == "1280x800");

                if (optimalWindowedResIndex == -1)
                    optimalWindowedResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == "1280x768");

                if (optimalWindowedResIndex > -1)
                {
                    ddClientResolution.SelectedIndex = optimalWindowedResIndex;
                }
            }
        }

        private void ChkWindowedMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkWindowedMode.Checked)
            {
                chkBorderlessWindowedMode.AllowChecking = true;
                return;
            }

            chkBorderlessWindowedMode.AllowChecking = false;
            chkBorderlessWindowedMode.Checked = false;
        }

        /// <summary>
        /// Loads the user's preferred renderer.
        /// </summary>
        private void LoadRenderer()
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            int defaultIndex = ddRenderer.Items.FindIndex(
                r => ((DirectDrawWrapper)r.Tag).InternalName == defaultRenderer);

            if (defaultIndex == -1)
                defaultIndex = 0;

            string renderer = UserINISettings.Instance.Renderer;

            if (string.IsNullOrEmpty(renderer))
            {
                // Use default
                ddRenderer.SelectedIndex = defaultIndex;
            }
            else
            {
                int index = ddRenderer.Items.FindIndex(
                    r => ((DirectDrawWrapper)r.Tag).InternalName == renderer);

                ddRenderer.SelectedIndex = index == -1 ? defaultIndex : index;
            }
        }

        public override void Load()
        {
            LoadRenderer();
            ddDetailLevel.SelectedIndex = UserINISettings.Instance.DetailLevel;

            string currentRes = UserINISettings.Instance.IngameScreenWidth.Value +
                "x" + UserINISettings.Instance.IngameScreenHeight.Value;

            int index = ddIngameResolution.Items.FindIndex(i => i.Text == currentRes);

            ddIngameResolution.SelectedIndex = index > -1 ? index : 0;

            // Wonder what this "Win8CompatMode" actually does..
            // Disabling it used to be TS-DDRAW only, but it was never enabled after 
            // you had tried TS-DDRAW once, so most players probably have it always
            // disabled anyway
            IniSettings.Win8CompatMode.Value = "No";

            var renderer = (DirectDrawWrapper)ddRenderer.SelectedItem.Tag;

            if (!renderer.IsDxWnd)
            {
                chkWindowedMode.Checked = UserINISettings.Instance.WindowedMode;
                chkBorderlessWindowedMode.Checked = UserINISettings.Instance.BorderlessWindowedMode;
            }
            else
            {
                // DxWnd needs to have the game's integrated windowed mode disabled
                // Instead it has its own controls in its INI file
                IniFile dxWndIni = new IniFile(ProgramConstants.GamePath + "dxwnd.ini");
                chkWindowedMode.Checked = dxWndIni.GetBooleanValue("DxWnd", "RunInWindow", false);
                chkBorderlessWindowedMode.Checked = dxWndIni.GetBooleanValue("DxWnd", "NoWindowFrame", false);
            }

            string currentClientRes = WindowManager.WindowWidth + "x" + WindowManager.WindowHeight;

            int clientResIndex = ddClientResolution.Items.FindIndex(i => (string)i.Tag == currentClientRes);

            ddClientResolution.SelectedIndex = clientResIndex > -1 ? clientResIndex : 0;

            chkBorderlessClient.Checked = UserINISettings.Instance.BorderlessWindowedClient;

            int selectedThemeIndex = ddClientTheme.Items.FindIndex(
                ddi => ddi.Text == UserINISettings.Instance.ClientTheme);
            ddClientTheme.SelectedIndex = selectedThemeIndex > -1 ? selectedThemeIndex : 0;

            fileSettingCheckBoxes.ForEach(chkBox => chkBox.Load());

#if YR
            chkBackBufferInVRAM.Checked = UserINISettings.Instance.BackBufferInVRAM;
#else
            chkBackBufferInVRAM.Checked = !UserINISettings.Instance.BackBufferInVRAM;

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client");

            if (regKey == null)
                return;

            object tsCompatFixValue = regKey.GetValue("TSCompatFixInstalled", "No");
            string tsCompatFixString = (string)tsCompatFixValue;

            if (tsCompatFixString == "Yes")
            {
                GameCompatFixInstalled = true;
                btnGameCompatibilityFix.Text = "Disable";
            }

            object fsCompatFixValue = regKey.GetValue("FSCompatFixInstalled", "No");
            string fsCompatFixString = (string)fsCompatFixValue;

            if (fsCompatFixString == "Yes")
            {
                FinalSunCompatFixInstalled = true;
                btnMapEditorCompatibilityFix.Text = "Disable";
            }

            object tsCompatFixDeclinedValue = regKey.GetValue("TSCompatFixDeclined", "No");

            if (((string)tsCompatFixDeclinedValue) == "Yes")
            {
                GameCompatFixDeclined = true;
            }

            //object fsCompatFixDeclinedValue = regKey.GetValue("FSCompatFixDeclined", "No");

            //if (((string)fsCompatFixDeclinedValue) == "Yes")
            //{
            //    FinalSunCompatFixDeclined = true;
            //}
#endif
        }

        public override bool Save()
        {
            bool restartRequired = false;

            IniSettings.DetailLevel.Value = ddDetailLevel.SelectedIndex;

            string[] resolution = ddIngameResolution.SelectedItem.Text.Split('x');

            int[] ingameRes = new int[2] { int.Parse(resolution[0]), int.Parse(resolution[1]) };

            IniSettings.IngameScreenWidth.Value = ingameRes[0];
            IniSettings.IngameScreenHeight.Value = ingameRes[1];

            // Calculate drag selection distance, scale it with resolution width
            int dragDistance = ingameRes[0] / ORIGINAL_RESOLUTION_WIDTH * DRAG_DISTANCE_DEFAULT;
            IniSettings.DragDistance.Value = dragDistance;

            var selectedRenderer = (DirectDrawWrapper)ddRenderer.SelectedItem.Tag;

            IniSettings.WindowedMode.Value = chkWindowedMode.Checked &&
                !selectedRenderer.IsDxWnd;

            IniSettings.BorderlessWindowedMode.Value = chkBorderlessWindowedMode.Checked &&
                !selectedRenderer.IsDxWnd;

            string[] clientResolution = ((string)ddClientResolution.SelectedItem.Tag).Split('x');

            int[] clientRes = new int[2] { int.Parse(clientResolution[0]), int.Parse(clientResolution[1]) };

            IniSettings.ClientResolutionX.Value = clientRes[0];
            IniSettings.ClientResolutionY.Value = clientRes[1];

            if (clientRes[0] != WindowManager.WindowWidth ||
                clientRes[1] != WindowManager.WindowHeight)
                restartRequired = true;

            if (IniSettings.BorderlessWindowedClient.Value != chkBorderlessClient.Checked)
                restartRequired = true;

            IniSettings.BorderlessWindowedClient.Value = chkBorderlessClient.Checked;

            if (IniSettings.ClientTheme != ddClientTheme.SelectedItem.Text)
                restartRequired = true;

            IniSettings.ClientTheme.Value = ddClientTheme.SelectedItem.Text;

#if YR
            IniSettings.BackBufferInVRAM.Value = chkBackBufferInVRAM.Checked;
#else
            IniSettings.BackBufferInVRAM.Value = !chkBackBufferInVRAM.Checked;
#endif

            fileSettingCheckBoxes.ForEach(chkBox => chkBox.Save());

            foreach (var renderer in renderers)
                renderer.Clean();

            selectedRenderer.Apply();

            if (selectedRenderer.IsDxWnd)
            {
                IniFile dxWndIni = new IniFile(ProgramConstants.GamePath + "dxwnd.ini");
                dxWndIni.SetBooleanValue("DxWnd", "RunInWindow", chkWindowedMode.Checked);
                dxWndIni.SetBooleanValue("DxWnd", "NoWindowFrame", chkBorderlessWindowedMode.Checked);
                dxWndIni.WriteIniFile();
            }

            IniSettings.Renderer.Value = selectedRenderer.InternalName;

#if !YR
            File.Delete(ProgramConstants.GamePath + "Language.dll");

            if (ingameRes[0] >= 1024 && ingameRes[1] >= 720)
                File.Copy(ProgramConstants.GamePath + "Resources\\language_1024x720.dll", ProgramConstants.GamePath + "Language.dll");
            else if (ingameRes[0] >= 800 && ingameRes[1] >= 600)
                File.Copy(ProgramConstants.GamePath + "Resources\\language_800x600.dll", ProgramConstants.GamePath + "Language.dll");
            else
                File.Copy(ProgramConstants.GamePath + "Resources\\language_640x480.dll", ProgramConstants.GamePath + "Language.dll");
#endif

            return restartRequired;
        }

        private List<ScreenResolution> GetResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            var screenResolutions = new List<ScreenResolution>();

            foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (dm.Width < minWidth || dm.Height < minHeight || dm.Width > maxWidth || dm.Height > maxHeight)
                    continue;

                var resolution = new ScreenResolution(dm.Width, dm.Height);

                // SupportedDisplayModes can include the same resolution multiple times
                // because it takes the refresh rate into consideration.
                // Which means that we have to check if the resolution is already listed
                if (screenResolutions.Find(res => res.Equals(resolution)) != null)
                    continue;

                screenResolutions.Add(resolution);
            }

            return screenResolutions;
        }

        /// <summary>
        /// A single screen resolution.
        /// </summary>
        sealed class ScreenResolution : IComparable<ScreenResolution>
        {
            public ScreenResolution(int width, int height)
            {
                Width = width;
                Height = height;
            }

            /// <summary>
            /// The width of the resolution in pixels.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// The height of the resolution in pixels.
            /// </summary>
            public int Height { get; set; }

            public override string ToString()
            {
                return Width + "x" + Height;
            }

            public int CompareTo(ScreenResolution res2)
            {
                if (this.Width < res2.Width)
                    return -1;
                else if (this.Width > res2.Width)
                    return 1;
                else // equal
                {
                    if (this.Height < res2.Height)
                        return -1;
                    else if (this.Height > res2.Height)
                        return 1;
                    else return 0;
                }
            }

            public override bool Equals(object obj)
            {
                var resolution = obj as ScreenResolution;

                if (resolution == null)
                    return false;

                return CompareTo(resolution) == 0;
            }

            public override int GetHashCode()
            {
                return Width - Height;
            }
        }
    }
}
