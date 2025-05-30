﻿using Client.Controls;
using Client.Envir;
using Client.Models;
using Client.UserModels;
using Library;
using Library.SystemModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using C = Library.Network.ClientPackets;

namespace Client.Scenes.Views
{
    public sealed  class BigMapDialog : DXWindow
    {
        #region Properties

        #region SelectedInfo

        public MapInfo SelectedInfo
        {
            get { return _SelectedInfo; }
            set
            {
                if (_SelectedInfo == value) return;

                MapInfo oldValue = _SelectedInfo;
                _SelectedInfo = value;

                OnSelectedInfoChanged(oldValue, value);
            }
        }
        private MapInfo _SelectedInfo;
        public event EventHandler<EventArgs> SelectedInfoChanged;
        public void OnSelectedInfoChanged(MapInfo oValue, MapInfo nValue)
        {
            SelectedInfoChanged?.Invoke(this, EventArgs.Empty);

            foreach (DXControl control in MapInfoObjects.Values)
                control.Dispose();

            MapInfoObjects.Clear();

            if (SelectedInfo == null) return;

            TitleLabel.Text = SelectedInfo.PlayerDescription;
            Image.Index = SelectedInfo.MiniMap;

            var minWidth = 320;
            var minHeight = 240;

            var maxWidth = 800;
            var maxHeight = 600;

            SetClientSize(Image.Size, minWidth, minHeight, maxWidth, maxHeight);

            var imageLargerThanPanel = (Image.Size.Width > maxWidth || Image.Size.Height > maxHeight);

            Image.Movable = imageLargerThanPanel;
            Image.IgnoreMoveBounds = imageLargerThanPanel;

            var locationX = (Image.Size.Width - Panel.Size.Width) / 2; 
            var locationY = (Image.Size.Height - Panel.Size.Height) / 2;

            Image.Location = new Point(-locationX, -locationY);

            Location = new Point((GameScene.Game.Size.Width - Size.Width) / 2, (GameScene.Game.Size.Height - Size.Height) / 2);

            Size size = GetMapSize(SelectedInfo.FileName);
            ScaleX = Image.Size.Width / (float)size.Width;
            ScaleY = Image.Size.Height / (float)size.Height;

            foreach (NPCInfo ob in Globals.NPCInfoList.Binding)
                Update(ob);

            foreach (MovementInfo ob in Globals.MovementInfoList.Binding)
                Update(ob);

            foreach (ClientObjectData ob in GameScene.Game.DataDictionary.Values)
                Update(ob);
        }

        public void ToggleOpen(bool open)
        {
            if (open)
            {
                if (!TryShowMap(GameScene.Game.MapControl.MapInfo))
                {
                    return;
                }

                Opacity = 1F;
                Visible = true;
            }
            else
            {
                Visible = false;
            }
        }

        public bool TryShowMap(MapInfo map, bool sendMessage = true)
        {
            if (map == null || map.MiniMap == 0)
            {
                if (sendMessage)
                    GameScene.Game.ReceiveChat("No map available.", MessageType.System);

                return false;
            }

            return true;
        }

        public void SetClientSize(Size clientSize, int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            SetClientSize(new Size(Math.Min(Math.Max(clientSize.Width, minWidth), maxWidth), Math.Min(Math.Max(clientSize.Height, minHeight), maxHeight)));
        }

        private Size GetMapSize(string fileName)
        {
            var path = Path.Combine(Config.MapPath, fileName + ".map");

            if (!File.Exists(path)) return Size.Empty;

            using (FileStream stream = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(22, SeekOrigin.Begin);
                
                return new Size(reader.ReadInt16(), reader.ReadInt16());
            }
        }

        #endregion
        
        public Rectangle Area;
        public DXImageControl Image;
        public DXControl Panel;
        
        public static float ScaleX, ScaleY;

        public Dictionary<object, DXControl> MapInfoObjects = new Dictionary<object, DXControl>();

        public override void OnClientAreaChanged(Rectangle oValue, Rectangle nValue)
        {
            base.OnClientAreaChanged(oValue, nValue);

            if (Panel == null) return;

            Panel.Location = ClientArea.Location;
            Panel.Size = ClientArea.Size;
        }
        public override void OnIsVisibleChanged(bool oValue, bool nValue)
        {
            base.OnIsVisibleChanged(oValue, nValue);

            SelectedInfo = IsVisible ? GameScene.Game.MapControl.MapInfo : null;

        }
        public override void OnOpacityChanged(float oValue, float nValue)
        {
            base.OnOpacityChanged(oValue, nValue);

            foreach (DXControl control in Controls)
                control.Opacity = Opacity;

            foreach (DXControl control in MapInfoObjects.Values)
                control.Opacity = Opacity;

            if (Image != null)
            {
                Image.Opacity = Opacity;
                Image.ImageOpacity = Opacity;
            }
        }


        public override WindowType Type => WindowType.None;
        public override bool CustomSize => false;
        public override bool AutomaticVisibility => false;

        #endregion

        public BigMapDialog()
        {
            BackColour = Color.Black;
            HasFooter = false;

            AllowResize = false;

            Panel = new DXControl
            {
                Parent = this,
                Location = Area.Location,
                Size = Area.Size
            };

            Image = new DXImageControl
            {
                Parent = Panel,
                LibraryFile = LibraryFile.MiniMap,
                Movable = true,
                IgnoreMoveBounds = true,
                Clip = true
            };
            Image.MouseClick += Image_MouseClick;
        }

        private void Image_MouseClick(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Right) == MouseButtons.Right)
            {
                int x = (int)((e.Location.X - Image.DisplayArea.X) / ScaleX);
                int y = (int)((e.Location.Y - Image.DisplayArea.Y) / ScaleY);
               
                CEnvir.Enqueue(new C.TeleportRing { Location = new Point(x, y), Index = SelectedInfo.Index });
            }
        }

        #region Methods
        public override void Draw()
        {
            if (!IsVisible || Size.Width == 0 || Size.Height == 0) return;

            OnBeforeDraw();
            DrawControl();
            OnBeforeChildrenDraw();
            DrawChildControls();
            DrawWindow();
            TitleLabel.Draw();
            DrawBorder();
            OnAfterDraw();
        }
        public void Update(NPCInfo ob)
        {
            if (SelectedInfo == null) return;

            DXControl control;

            if (!MapInfoObjects.TryGetValue(ob, out control))
            {
                if (ob.Region?.Map != SelectedInfo) return;

                control = GameScene.Game.GetNPCControl(ob);
                control.Parent = Image;
                control.Opacity = Opacity;
                MapInfoObjects[ob] = control;
            }
            else if ((CurrentQuest)control.Tag == ob.CurrentQuest) return;

                control.Dispose();
                MapInfoObjects.Remove(ob);
            if (ob.Region?.Map != SelectedInfo)  return;

            control = GameScene.Game.GetNPCControl(ob);
            control.Parent = Image;
            control.Opacity = Opacity;
            MapInfoObjects[ob] = control;

            Size size = GetMapSize(SelectedInfo.FileName);

            if (ob.Region.PointList == null)
                ob.Region.CreatePoints(size.Width);

            int minX = size.Width, maxX = 0, minY = size.Height, maxY = 0;

            foreach (Point point in ob.Region.PointList)
            {
                if (point.X < minX)
                    minX = point.X;
                if (point.X > maxX)
                    maxX = point.X;

                if (point.Y < minY)
                    minY = point.Y;
                if (point.Y > maxY)
                    maxY = point.Y;
            }

            int x = (minX + maxX) / 2;
            int y = (minY + maxY) / 2;


            control.Location = new Point((int)(ScaleX * x) - control.Size.Width / 2, (int)(ScaleY * y) - control.Size.Height / 2);
        }
        public void Update(MovementInfo ob)
        {
            if (ob.SourceRegion == null || ob.SourceRegion.Map != SelectedInfo) return;
            if (ob.DestinationRegion?.Map == null || ob.Icon == MapIcon.None) return;

            if (GameScene.Game.MapControl.InstanceInfo != null)
            {
                if (!GameScene.Game.MapControl.InstanceInfo.Maps.Any(m => m.Map == ob.SourceRegion.Map) && ob.NeedInstance == null) return;
                if (!GameScene.Game.MapControl.InstanceInfo.Maps.Any(m => m.Map == ob.DestinationRegion?.Map) && ob.NeedInstance == null) return;
            }

            Size size = GetMapSize(SelectedInfo.FileName);

            if (ob.SourceRegion.PointList == null)
                ob.SourceRegion.CreatePoints(size.Width);

            int minX = size.Width, maxX = 0, minY = size.Height, maxY = 0;

            foreach (Point point in ob.SourceRegion.PointList)
            {
                if (point.X < minX)
                    minX = point.X;
                if (point.X > maxX)
                    maxX = point.X;

                if (point.Y < minY)
                    minY = point.Y;
                if (point.Y > maxY)
                    maxY = point.Y;
            }

            int x = (minX + maxX)/2;
            int y = (minY + maxY)/2;


            DXImageControl control;
            MapInfoObjects[ob] = control = new DXImageControl
            {
                LibraryFile = LibraryFile.MiniMapIcon,
                Parent = Image,
                Opacity =  Opacity,
                ImageOpacity =  Opacity,
                Hint = ob.DestinationRegion.Map.PlayerDescription,
            };
            control.OpacityChanged += (o, e) => control.ImageOpacity = control.Opacity;

            GameScene.Game.UpdateMapIcon(control, ob.Icon);

            control.MouseClick += (o, e) =>
            {
                if (!TryShowMap(ob.DestinationRegion.Map))
                {
                    return;
                }

                SelectedInfo = ob.DestinationRegion.Map;
            };
            control.Location = new Point((int) (ScaleX*x) - control.Size.Width/2, (int) (ScaleY*y) - control.Size.Height/2);
        }
        public void Update(ClientObjectData ob)
        {
            if (SelectedInfo == null) return;

            DXControl control;

            if (!MapInfoObjects.TryGetValue(ob, out control))
            {
                if (ob.MapIndex != SelectedInfo.Index) return;
                if (ob.ItemInfo != null && ob.ItemInfo.Rarity == Rarity.Common) return;
                if (ob.MonsterInfo != null && (ob.Dead || ob.MonsterInfo.Image == MonsterImage.None)) return;

                MapInfoObjects[ob] = control = new DXControl
                {
                    DrawTexture = true,
                    Parent = Image,
                    Opacity =  Opacity,
                };
            }
            else if (ob.MapIndex != SelectedInfo.Index || (ob.MonsterInfo != null && ob.Dead) || (ob.ItemInfo != null && ob.ItemInfo.Rarity == Rarity.Common))
            {
                control.Dispose();
                MapInfoObjects.Remove(ob);
                return;
            }
            
            Size size = new Size(3, 3);
            Color colour = Color.White;
            string name = ob.Name;

            if (ob.MonsterInfo != null)
            {
                name = $"{ob.MonsterInfo.MonsterName}";
                if (ob.MonsterInfo.AI < 0)
                {
                    colour = Color.LightBlue;
                }
                else
                {
                    colour = Color.Red;

                    if (GameScene.Game.HasQuest(ob.MonsterInfo, GameScene.Game.MapControl.MapInfo))
                        colour = Color.Orange;
                }

                if (ob.MonsterInfo.Flag == MonsterFlag.CastleObjective || ob.MonsterInfo.Flag == MonsterFlag.CastleDefense)
                {
                    control.Visible = false;
                }

                if (ob.MonsterInfo.IsBoss)
                {
                    size = new Size(5, 5);

                    if (control.Controls.Count == 0) // This is disgusting but its cheap
                    {
                        new DXControl
                        {
                            Parent = control,
                            Location = new Point(1, 1),
                            BackColour = colour,
                            DrawTexture = true,
                            Size = new Size(3, 3)
                        };
                    }
                    else
                        control.Controls[0].BackColour = colour;

                    colour = Color.White;
                }

                if (!string.IsNullOrEmpty(ob.PetOwner))
                {
                    name += $" ({ob.PetOwner})";
                    control.DrawTexture = false;
                }
            }
            else if (ob.ItemInfo != null)
            {
                colour = Color.DarkBlue;
            }
            else
            {
                if (MapObject.User.ObjectID == ob.ObjectID)
                {
                    colour = Color.Cyan;
                }
                else if (GameScene.Game.Observer)
                {
                    control.Visible = false;
                }
                else if (GameScene.Game.GroupBox.Members.Any(x => x.ObjectID == ob.ObjectID))
                {
                    colour = Color.Blue;
                }
                else if (GameScene.Game.Partner != null && GameScene.Game.Partner.ObjectID == ob.ObjectID) 
                {
                    colour = Color.DeepPink;
                }
                else if (GameScene.Game.GuildBox.GuildInfo != null && GameScene.Game.GuildBox.GuildInfo.Members.Any(x => x.ObjectID == ob.ObjectID))
                {
                    colour = Color.DeepSkyBlue;
                }
            }

            control.Hint = name;
            control.BackColour = colour;
            control.Size = size;
            control.Location = new Point((int) (ScaleX*ob.Location.X) - size.Width/2, (int) (ScaleY*ob.Location.Y) - size.Height/2);
        }

        public void Remove(object ob)
        {
            DXControl control;

            if (!MapInfoObjects.TryGetValue(ob, out control)) return;

            control.Dispose();
            MapInfoObjects.Remove(ob);
        }
        
        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _SelectedInfo = null;
                SelectedInfoChanged = null;

                Area = Rectangle.Empty;
                ScaleX = 0;
                ScaleY = 0;

                foreach (KeyValuePair<object, DXControl> pair in MapInfoObjects)
                {
                    if (pair.Value == null) continue;
                    if (pair.Value.IsDisposed) continue;

                    pair.Value.Dispose();
                }

                MapInfoObjects.Clear();
                MapInfoObjects = null;


                if (Image != null)
                {
                    if (!Image.IsDisposed)
                        Image.Dispose();

                    Image = null;
                }
                
                if (Panel != null)
                {
                    if (!Panel.IsDisposed)
                        Panel.Dispose();

                    Panel = null;
                }
            }
        }

        #endregion
    }
}
