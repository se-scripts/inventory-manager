using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /*
        * R e a d m e
        * -----------
        * Production equipment inventory management for LCD.
        * 太空工程师，生产设备库存管理脚本， 自动将装配机精炼厂的产物，放进储物箱子。
        * 
        * @see <https://github.com/se-scripts/produce-inventory-manager>
        * @author [Hi.James](https://space.bilibili.com/368005035)
        * @author [li-guohao](https://github.com/li-guohao)
        */
        MyIni _ini = new MyIni();

        List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<IMyRefinery> refineries = new List<IMyRefinery>();

        int counter_InventoryManagement = 0, counter_AssemblerManagement = 0, counter_RefineryManagement = 0;
        double counter_Logo = 0;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GridTerminalSystem.GetBlocksOfType(cargoContainers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(assemblers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(refineries, b => b.IsSameConstructAs(Me) && !b.BlockDefinition.ToString().Contains("Shield"));

            ProgrammableBlockScreen();
        }

        public void DebugLCD(string text)
        {
            List<IMyTextPanel> debugPanel = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(debugPanel, b => b.IsSameConstructAs(Me) && b.CustomName == "DEBUGLCD");

            if (debugPanel.Count == 0) return;

            string temp = "";
            foreach (var panel in debugPanel)
            {
                temp = "";
                temp = panel.GetText();
            }

            foreach (var panel in debugPanel)
            {
                if (panel.ContentType != ContentType.TEXT_AND_IMAGE) panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.55f;
                panel.Font = "LoadingScreen";
                panel.WriteText(DateTime.Now.ToString(), false);
                panel.WriteText("\n", true);
                panel.WriteText(text, true);
                panel.WriteText("\n", true);
                panel.WriteText(temp, true);
            }
        }

        public void Assembler_to_CargoContainers()
        {
            Echo("Assembler_to_CargoContainers");

            for (int i = 0; i < 10; i++)
            {
                int assemblerCounter = counter_AssemblerManagement * 10 + i;

                if (assemblerCounter >= assemblers.Count)
                {
                    counter_AssemblerManagement = 0;
                    return;
                }
                else
                {
                    var assembler = assemblers[assemblerCounter];
                    double currentVolume_Double = ((double)assembler.OutputInventory.CurrentVolume);
                    double maxVolume_Double = ((double)assembler.OutputInventory.MaxVolume);
                    double volumeRatio_Double = currentVolume_Double / maxVolume_Double;

                    if (assembler.IsProducing)
                    {
                        if (volumeRatio_Double > 0)
                        {
                            List<MyInventoryItem> items = new List<MyInventoryItem>();
                            assembler.OutputInventory.GetItems(items);
                            foreach (var item in items)
                            {
                                foreach (var cargoContainer in cargoContainers) {
                                    if (!cargoContainer.GetInventory().CanPutItems) { continue; }
                                    if (!assembler.OutputInventory.CanTransferItemTo(cargoContainer.GetInventory(), item.Type)) { continue; }
                                    assembler.OutputInventory.TransferItemTo(cargoContainer.GetInventory(), item);
                                }
                            }
                        }

                    }
                    else {
                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        assembler.InputInventory.GetItems(items);

                        if (assembler.InputInventory.ItemCount > 0) {
                            foreach (var cargo in cargoContainers)
                            {
                                var cargoIventory = cargo.GetInventory();
                                if (cargoIventory.IsFull || !cargoIventory.CanPutItems) { continue; }
                                foreach (var item in items)
                                {
                                    if (!assembler.InputInventory.CanTransferItemTo(cargoIventory, item.Type)) { continue; }
                                    assembler.InputInventory.TransferItemTo(cargoIventory, item);
                                }
                                    
                            }

                        }
                        items.Clear();


                        assembler.OutputInventory.GetItems(items);

                        if (assembler.OutputInventory.ItemCount > 0) {
                            foreach (var cargo in cargoContainers) {
                                var cargoIventory = cargo.GetInventory();
                                if (cargoIventory.IsFull || !cargoIventory.CanPutItems) { continue; }
                                foreach (var item in items) {
                                    if (!assembler.OutputInventory.CanTransferItemTo(cargoIventory, item.Type)) { continue; }
                                    assembler.OutputInventory.TransferItemTo(cargoIventory, item);
                                }
                            }

                        }
                    }
                }
            }

            counter_AssemblerManagement++;
        }// Assembler_to_CargoContainers END!

        public void ShowProductionQueue()
        {
            StringBuilder str = new StringBuilder();
            foreach (var assembler in assemblers)
            {
                List<MyProductionItem> productionitems = new List<MyProductionItem>();
                assembler.GetQueue(productionitems);
                foreach (var item1 in productionitems)
                {
                    str.Append($"{assembler.CustomName}:{item1.BlueprintId}:{item1.Amount}");
                    str.Append("\n");
                }
            }
            DebugLCD(str.ToString());
        }

        public void Refinery_to_CargoContainers()
        {
            Echo("Refinery_to_CargoContainers");
            for (int i = 0; i < 10; i++)
            {
                int refineryCounter = counter_RefineryManagement * 10 + i;

                if (refineryCounter >= refineries.Count)
                {
                    counter_RefineryManagement = 0;
                    return;
                }
                else
                {
                    if (refineries[refineryCounter].IsProducing == false)
                    {
                        foreach (var cargoContainer in cargoContainers)
                        {
                            List<MyInventoryItem> items1 = new List<MyInventoryItem>();
                            refineries[refineryCounter].InputInventory.GetItems(items1);
                            foreach (var item in items1)
                            {
                                bool tf = refineries[refineryCounter].InputInventory.TransferItemTo(cargoContainer.GetInventory(), item);
                            }

                            List<MyInventoryItem> items2 = new List<MyInventoryItem>();
                            refineries[refineryCounter].OutputInventory.GetItems(items2);
                            foreach (var item in items2)
                            {
                                bool tf = refineries[refineryCounter].OutputInventory.TransferItemTo(cargoContainer.GetInventory(), item);
                            }

                            items1.Clear();
                            items2.Clear();
                            refineries[refineryCounter].InputInventory.GetItems(items1);
                            refineries[refineryCounter].OutputInventory.GetItems(items2);
                            if (items1.Count < 1 && items2.Count < 1) break;
                        }
                    }
                    else
                    {
                        foreach (var cargoContainer in cargoContainers)
                        {
                            //List<MyInventoryItem> items1 = new List<MyInventoryItem>();
                            //refineries[refineryCounter].InputInventory.GetItems(items1);
                            //foreach (var item in items1)
                            //{
                            //    string str = item.Type.ToString();
                            //    if (str.IndexOf("_Ore") != -1)
                            //    {
                            //        bool tf = refineries[refineryCounter].InputInventory.TransferItemTo(cargoContainer.GetInventory(), item);
                            //    }
                            //}

                            List<MyInventoryItem> items2 = new List<MyInventoryItem>();
                            refineries[refineryCounter].OutputInventory.GetItems(items2);
                            foreach (var item in items2)
                            {
                                bool tf = refineries[refineryCounter].OutputInventory.TransferItemTo(cargoContainer.GetInventory(), item);
                            }
                            items2.Clear();
                            refineries[refineryCounter].OutputInventory.GetItems(items2);
                            if (items2.Count < 1) break;
                        }
                    }
                }
            }
            counter_RefineryManagement++;
        }// Refinery_to_CargoContainers END!

        public void ProgrammableBlockScreen()
        {

            //  512 X 320
            IMyTextSurface panel = Me.GetSurface(0);

            if (panel == null) return;
            panel.ContentType = ContentType.SCRIPT;

            MySpriteDrawFrame frame = panel.DrawFrame();

            float x = 512 / 2, y1 = 205;
            DrawLogo(frame, x, y1, 200);
            PanelWriteText(frame, "Production inventory management\nby Hi.James and li-guohao.", x, y1 + 110, 1f, TextAlignment.CENTER);

            frame.Dispose();

        }

        public void PanelWriteText(MySpriteDrawFrame frame, string text, float x, float y, float fontSize, TextAlignment alignment)
        {
            MySprite sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(x, y),
                RotationOrScale = fontSize,
                Color = Color.Coral,
                Alignment = alignment,
                FontId = "LoadingScreen"
            };
            frame.Add(sprite);
        }

        public void DrawLogo(MySpriteDrawFrame frame, float x, float y, float width)
        {
            MySprite sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = new Vector2(x, y),
                Size = new Vector2(width - 6, width - 6),
                RotationOrScale = Convert.ToSingle(counter_Logo / 360 * 2 * Math.PI),
                Alignment = TextAlignment.CENTER,
            };
            frame.Add(sprite);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = new Vector2(x, y),
                Size = new Vector2(width / 2, width / 2),
                RotationOrScale = Convert.ToSingle(2 * Math.PI - counter_Logo / 360 * 2 * Math.PI),
                Alignment = TextAlignment.CENTER,
            };
            frame.Add(sprite);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = new Vector2(x, y),
                Size = new Vector2(width / 4, width / 4),
                RotationOrScale = Convert.ToSingle(Math.PI + counter_Logo / 360 * 2 * Math.PI),
                Alignment = TextAlignment.CENTER,
            };
            frame.Add(sprite);

        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{DateTime.Now}");
            Echo("Program is running.");

            DateTime beforDT = System.DateTime.Now;

            if (counter_InventoryManagement++ >= 4) counter_InventoryManagement = 1;

            switch (counter_InventoryManagement)
            {
                case 1:
                    Assembler_to_CargoContainers();
                    ShowProductionQueue();
                    break;
                case 2:
                    Refinery_to_CargoContainers();
                    break;
                case 3:
                    Assembler_to_CargoContainers();
                    break;
            }


            DateTime afterDT = System.DateTime.Now;
            TimeSpan ts = afterDT.Subtract(beforDT);
            Echo("Total cost ms：" + ts.TotalMilliseconds);

            DebugLCD("Total cost ms: " + ts.TotalMilliseconds);
        }
    }
}
