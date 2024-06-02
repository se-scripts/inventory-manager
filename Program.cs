using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
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
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        List<IMyCryoChamber> cryoChambers = new List<IMyCryoChamber>();
        List<IMyCockpit> cockpits = new List<IMyCockpit>();

        int counter_InventoryManagement = 0;
        double counter_Logo = 0;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GridTerminalSystem.GetBlocksOfType(cargoContainers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(assemblers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(refineries, b => b.IsSameConstructAs(Me) && !b.BlockDefinition.ToString().Contains("Shield"));
            GridTerminalSystem.GetBlocksOfType(connectors, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(cryoChambers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(cockpits, b => b.IsSameConstructAs(Me));

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

        public void AssemblerToCargoContainers()
        {
            Echo("AssemblerToCargoContainers");
            foreach (var assembler in assemblers) {
                if (!assembler.IsProducing)
                {
                    TransferToCargoContainers(assembler.InputInventory);
                }
                TransferToCargoContainers(assembler.OutputInventory);
            }
        }// Assembler_to_CargoContainers END!

        public void TransferItems(IMyInventory from, IMyInventory to)
        {
            if (from == null || to == null) { return; }
            if (from.ItemCount == 0) { return; }
            if (to.IsFull || !to.CanPutItems) { return; }
            List<MyInventoryItem> items = new List<MyInventoryItem>(from.ItemCount);
            from.GetItems(items);
            foreach (var item in items) {
                if (!from.CanTransferItemTo(to, item.Type)) { continue; }
                from.TransferItemTo(to, item);
            }
        }

        public void TransferToCargoContainers(IMyInventory from) { 
            if (from == null ) { return; }
            if (from.ItemCount == 0) { return; }
            if (cargoContainers == null || cargoContainers.Count == 0) { return;}
            foreach (var cargo in cargoContainers) {
                TransferItems(from, cargo.GetInventory());
            }
        }

        public void RefineryToCargoContainers()
        {
            Echo("RefineryToCargoContainers");
            foreach (var refinery in refineries) {
                if (!refinery.IsProducing)
                {
                    TransferToCargoContainers(refinery.InputInventory);
                }
                TransferToCargoContainers(refinery.OutputInventory);
            }
        }// Refinery_to_CargoContainers END!

        public void ConnectorToCargoContainers()
        {
            Echo("ConnectorToCargoContainers");
            foreach (var connector in connectors) {
                TransferToCargoContainers(connector.GetInventory());
            }
        }

        public void CockpitToCargoContainers() {
            Echo("CockpitToCargoContainers");
            foreach (var cockpit in cockpits) { 
                TransferToCargoContainers(cockpit.GetInventory());
            }
        }

        public void ProgrammableBlockScreen()
        {

            //  512 X 320
            IMyTextSurface panel = Me.GetSurface(0);

            if (panel == null) return;
            panel.ContentType = ContentType.SCRIPT;

            MySpriteDrawFrame frame = panel.DrawFrame();

            float x = 512 / 2, y1 = 205;
            DrawLogo(frame, x, y1, 200);
            PanelWriteText(frame, "Inventory management\nby Hi.James and li-guohao.", x, y1 + 110, 1f, TextAlignment.CENTER);

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

            if (counter_InventoryManagement++ >= 5) counter_InventoryManagement = 1;

            switch (counter_InventoryManagement)
            {
                case 1:
                    AssemblerToCargoContainers();
                    break;
                case 2:
                    RefineryToCargoContainers();
                    break;
                case 3:
                    ConnectorToCargoContainers();
                    break;
                case 4:
                    CockpitToCargoContainers();
                    break;
            }


            DateTime afterDT = System.DateTime.Now;
            TimeSpan ts = afterDT.Subtract(beforDT);
            Echo("Total cost ms：" + ts.TotalMilliseconds);

            DebugLCD("Total cost ms: " + ts.TotalMilliseconds);
        }
    }
}
