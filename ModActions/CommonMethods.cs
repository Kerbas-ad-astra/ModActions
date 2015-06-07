﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace ModActions
{

    public class ModActionData
    {
        public int Identifier; //unique identifier, used for SWITCH statement
        public string ModuleName; //partModule name for filter, show this action if selected part has this module
        public string Description; //action group description, used for BaseAction.guiName, first column (is editable), resets when ActionValue type changes to default action value
        public string Name; //Mod name, second column (is selectable)
        public string ActionGroup; //Action group, third column (is selectable), break up large mods such as mechjeb into regions
        public string ActionActual; //our actual action, fourth column
        public string ActionValue; //default value, if non-editable display as lable, otherwise as text box, fifth column
        public string ActionDataType; //type of last column?

        public override string ToString() //give this class a useful ToString() function
        {
            return Identifier.ToString() + " " + ModuleName + " " + Description + " " + Name + " " + ActionGroup + " " + ActionActual + " " + ActionValue + " " + ActionDataType.ToString();
        }

        public ModActionData() //blank constructor
        {

        }

        public ModActionData(ModActionData orig) //copy constructor
        {
            //ModActionData copy = new ModActionData();
            Identifier = orig.Identifier;
            ModuleName = string.Copy(orig.ModuleName);
            Description = string.Copy(orig.Description);
            Name = string.Copy(orig.Name);
            ActionGroup = string.Copy(orig.ActionGroup);
            ActionActual = string.Copy(orig.ActionActual);
            ActionValue = string.Copy(orig.ActionValue);
            ActionDataType = string.Copy(orig.ActionDataType);

        }

    }


    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class ModActionsMainMenu : PartModule
    {
        public void Start()
        {
            Debug.Log("ModActions Ver. 0.1 Starting.....");
            if (!StaticMethods.ListPopulated) //populate our list if this is first load
            {
                StaticMethods.AllActionsList = new List<ModActionData>();
                string[] configFiles = Directory.GetFiles(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName + "GameData/Diazo/ModActions"); //full path of all files in save dir
                foreach (string str in configFiles)
                {
                    if (str.EndsWith(".actions"))
                    {
                        ConfigNode loadingNode = ConfigNode.Load(str);
                        foreach (AssemblyLoader.LoadedAssembly Asm in AssemblyLoader.loadedAssemblies)
                        {
                            if (Asm.dllName == loadingNode.GetValue("assemblyname"))
                            {
                                string modName = loadingNode.GetValue("modname");
                                string pmName = loadingNode.GetValue("pmname");
                                foreach (ConfigNode actNode in loadingNode.nodes)
                                {
                                    string actgroup = actNode.GetValue("name");
                                    foreach (ConfigNode typeNode in actNode.nodes)
                                    {
                                        StaticMethods.AllActionsList.Add(new ModActionData() { Identifier = int.Parse(typeNode.GetValue("ident")), ModuleName = pmName, Description = "", Name = modName, ActionGroup = actgroup, ActionActual = typeNode.GetValue("name"), ActionValue = typeNode.GetValue("data"), ActionDataType = typeNode.GetValue("ActionData") });
                                    }
                                }
                                loadingNode.SetValue("assemblyname", "gibberishToPreventThisFileFromLoadingTwice");
                            }
                        }

                    }
                }
                StaticMethods.ListPopulated = true;
                foreach (ModActionData mData in StaticMethods.AllActionsList) //for debugging, lists all actions
                {
                    Debug.Log("test " + mData.ToString());
                }
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ModActionsEditor : PartModule
    {
        MainGUIWindow ourWin;
        ConfigNode settings;
        float winTop;
        float winLeft;
        Part lastSelectedPart;
        IButton MABtn;
        bool showWin;
        ApplicationLauncherButton ModActsEditorButton;
        float lastUpdateTime;

        public void Start()
        {
            EditorPanels.Instance.actions.AddValueChangedDelegate(WinChangeAction);
            settings = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/Diazo/ModActions/ModActions.cfg");
            winTop = float.Parse(settings.GetValue("EdWinTop"));
            winLeft = float.Parse(settings.GetValue("EdWinLeft"));
            if (ToolbarManager.ToolbarAvailable) //check if toolbar available, load if it is
            {

                MABtn = ToolbarManager.Instance.add("ModActs", "MABtn");
                MABtn.TexturePath = "Diazo/ModActions/Btn";
                MABtn.ToolTip = "Mod Actions";
                MABtn.OnClick += (e) =>
                {
                    showWin = !showWin;
                    if (ourWin != null)
                    {
                        ourWin.drawWin = showWin;
                    }
                };
            }
            else
            {
                //now using stock toolbar as fallback
                ModActsEditorButton = ApplicationLauncher.Instance.AddModApplication(onStockToolbarClick, onStockToolbarClick, DummyVoid, DummyVoid, DummyVoid, DummyVoid, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, (Texture)GameDatabase.Instance.GetTexture("Diazo/ModActs/Btn", false));
            }
        }

        public void onStockToolbarClick()
        {
            showWin = !showWin;
            if (ourWin != null)
            {
                ourWin.drawWin = showWin;
            }
        }

        public void DummyVoid()
        {

        }

        public void OnDisable()
        {
            winTop = ourWin.MainWindowRect.y;
            winLeft = ourWin.MainWindowRect.x;
            settings.RemoveValue("EdWinTop");
            settings.RemoveValue("EdWinLeft");
            settings.AddValue("EdWinTop", winTop);
            settings.AddValue("EdWinLeft", winLeft);
            settings.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/ModActions/ModActions.cfg");
            if (ourWin != null)
            {
                ourWin.Kill();
            }
            ourWin = null;
            if (ToolbarManager.ToolbarAvailable) //if toolbar loaded, destroy button on leaving scene
            {
                MABtn.Destroy();
            }
            else
            {
                ApplicationLauncher.Instance.RemoveModApplication(ModActsEditorButton);
            }
        }

        public void WinChangeAction(IUIObject obj)
        {
            if (EditorLogic.fetch.editorScreen == EditorScreen.Actions)
            {
                if (ourWin == null) //initialize our window if not already extant, this event triggers twice per panels change
                {
                    ourWin = new MainGUIWindow(EditorLogic.SortedShipList, winTop, winLeft);
                    ourWin.drawWin = showWin;
                    try //getselectedparts returns null somewhere above it in the hierchy, do it this way for simplicities sake
                    {
                        ourWin.SetPart(EditorActionGroups.Instance.GetSelectedParts().First());
                        lastSelectedPart = EditorActionGroups.Instance.GetSelectedParts().First();
                    }
                    catch
                    {
                        ourWin.SetPart(null);
                        lastSelectedPart = null;
                    }
                }
            }
            else //moving away from actions panel, null our window
            {
                if (ourWin != null)
                {

                    ourWin.Kill();
                }
                ourWin = null;
            }
        }
        public void Update()
        {
            if (EditorLogic.fetch.editorScreen == EditorScreen.Actions)
            {
                try
                {
                    if (EditorActionGroups.Instance.GetSelectedParts().First() != lastSelectedPart) //check if selected part has changed
                    {
                        ourWin.SetPart(EditorActionGroups.Instance.GetSelectedParts().First());
                        lastSelectedPart = EditorActionGroups.Instance.GetSelectedParts().First();
                    }
                }
                catch //error trap if GetSelecetedParts above is null,
                {
                    //do nothing, if null nothing should happen
                }

                if (Time.time > lastUpdateTime + 5 && ourWin != null)
                {
                    ourWin.UpdateCheck();
                    lastUpdateTime = Time.time;
                }

            }
            if (ourWin != null)
            {
                ourWin.Update();
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ModActionsFlight : PartModule
    {
        MainGUIWindow ourWin;
        ConfigNode settings;
        float winTop;
        float winLeft;
        Part lastSelectedPart;
        bool ShowModActs;
        IButton MABtn;
        ApplicationLauncherButton ModActsEditorButton;
        float lastUpdateTime;

        public void Start()
        {

            settings = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/Diazo/ModActions/ModActions.cfg");
            winTop = float.Parse(settings.GetValue("FltWinTop"));
            winLeft = float.Parse(settings.GetValue("FltWinLeft"));
            if (ToolbarManager.ToolbarAvailable) //check if toolbar available, load if it is
            {

                MABtn = ToolbarManager.Instance.add("ModActs", "MABtn");
                MABtn.TexturePath = "Diazo/ModActions/Btn";
                MABtn.ToolTip = "Mod Actions";
                MABtn.OnClick += (e) =>
                {
                    ShowModActs = !ShowModActs;
                    if (ShowModActs)
                    {
                        if (ourWin == null)
                        {
                            ourWin = new MainGUIWindow(FlightGlobals.ActiveVessel.Parts, winTop, winLeft);
                        }
                        ourWin.SetPart(null);
                        lastSelectedPart = null;
                        ourWin.drawWin = ShowModActs;
                    }
                    else
                    {
                        if (ourWin != null)
                        {
                            ourWin.Kill();
                            ourWin = null;
                        }
                    }
                };
            }
            else
            {
                //now using stock toolbar as fallback
                ModActsEditorButton = ApplicationLauncher.Instance.AddModApplication(onStockToolbarClick, onStockToolbarClick, DummyVoid, DummyVoid, DummyVoid, DummyVoid, ApplicationLauncher.AppScenes.FLIGHT, (Texture)GameDatabase.Instance.GetTexture("Diazo/ModActs/Btn", false));
            }
        }

        public void onStockToolbarClick()
        {
            ShowModActs = !ShowModActs;
            if (ShowModActs)
            {
                if (ourWin != null)
                {
                    ourWin = new MainGUIWindow(FlightGlobals.ActiveVessel.Parts, winTop, winLeft);
                }
                ourWin.SetPart(null);
                lastSelectedPart = null;
                ourWin.drawWin = ShowModActs;
            }
            else
            {
                if (ourWin != null)
                {
                    ourWin.Kill();
                    ourWin = null;
                }
            }

        }

        public void DummyVoid()
        {

        }

        public void OnDisable()
        {
            winTop = ourWin.MainWindowRect.y;
            winLeft = ourWin.MainWindowRect.x;
            settings.RemoveValue("FltWinTop");
            settings.RemoveValue("FltWinLeft");
            settings.AddValue("FltWinTop", winTop);
            settings.AddValue("FltWinLeft", winLeft);
            settings.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/ModActions/ModActions.cfg");
            if (ourWin != null)
            {
                ourWin.Kill();
            }
            ourWin = null;
            if (ToolbarManager.ToolbarAvailable) //if toolbar loaded, destroy button on leaving scene
            {
                MABtn.Destroy();
            }
            else
            {
                ApplicationLauncher.Instance.RemoveModApplication(ModActsEditorButton);
            }
        }


        public void Update()
        {

            if (ourWin != null)
            {
                if (ourWin.selectedPart != lastSelectedPart) //check if selected part has changed
                {
                    ourWin.SetPart(lastSelectedPart);
                }

                //if (ourWin != null)
                //{
                //    ourWin.Update();
                //}

                if (Time.time > lastUpdateTime + 5 && ourWin != null)
                {
                    ourWin.UpdateCheck();
                    lastUpdateTime = Time.time;
                }

                if (Input.GetKeyDown(KeyCode.Mouse0) && ShowModActs)
                {

                    Part selPart = new Part();
                    selPart = SelectPartUnderMouse();
                    if (selPart != null)
                    {
                        lastSelectedPart = selPart;
                    }
                }
            }

        }

        public Part SelectPartUnderMouse()
        {
            FlightCamera CamTest = new FlightCamera();
            CamTest = FlightCamera.fetch;
            Ray ray = CamTest.mainCamera.ScreenPointToRay(Input.mousePosition);
            LayerMask RayMask = new LayerMask();
            RayMask = 1 << 0;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayMask))
            {
                Part hitPart = (Part)UIPartActionController.GetComponentUpwards("Part", hit.collider.gameObject); //how to find small parts that are "inside" the large part they are attached to.
                if (FlightGlobals.ActiveVessel.parts.Contains(hitPart))
                {
                    return hitPart;
                }
                else
                {
                    return null;
                }
                //return FlightGlobals.ActiveVessel.Parts.Find(p => p.gameObject == hit.transform.gameObject);
            }
            return null;
        }
    }

    public static class StaticMethods //static data that should never change
    {
        public static bool ListPopulated = false;
        public static List<ModActionData> AllActionsList;
    }


}