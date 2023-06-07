using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows.Shapes;
using System.Xml.Linq;
using ExtractorSharp.Core.Command;
using ExtractorSharp.Core.Composition;
using ExtractorSharp.Core.Model;

namespace ExtractorSharp.Command.ImageCommand {
    /// <summary>
    ///     保存贴图
    ///     不可撤销
    ///     可宏命令
    /// </summary>
    internal class SaveImage : ISingleAction, ICommandMessage {
        private Album Album { set; get; }
        private int Digit { set; get; }
        private bool FullPath { set; get; }
        private int Increment { set; get; }
        private bool AllImage { set; get; }

        /// <summary>
        ///     提取模式
        ///     <para>0.单张贴图</para>
        ///     <para>其他.多张贴图</para>
        /// </summary>
        private int Mode { set; get; }

        private SpriteEffect OnSaving { set; get; }

        private string Path { set; get; }

        private string Prefix { set; get; } = string.Empty;

        public int[] Indices { set; get; }

        public void Do(params object[] args) {
            Album = args[0] as Album;
            Mode = (int) args[1];
            Indices = args[2] as int[];
            Path = args[3] as string;
            if (args.Length > 4) {
                Prefix = (args[4] as string).Replace("\\", "/");
                Increment = (int) args[5];
                Digit = (int) args[6];
                FullPath = (bool) args[7];
                OnSaving = args[8] as SpriteEffect;
            }
            if (args.Length > 9) {
                AllImage = (bool)args[9];
            }
            Action(Album, Indices);
        }

        public void Redo() {
            // Method intentionally left empty.
        }

        public void Undo() {
            // Method intentionally left empty.
        }

        public void Action(Album album, int[] indexes) {
            if (Mode == 0) {
                //当保存模式为单张贴图时
                album.List[indexes[0]].Picture.Save(Path);
            } else {
                //是否加入文件的路径
                var dir = $"{Path}/{(FullPath ? album.Path : album.Name)}/{Prefix}";
                dir = dir.Replace('\\', '/');
                var index = dir.LastIndexOf("/");
                dir = dir.Substring(0, index + 1);
                var prefix = dir.Substring(index);
                if (File.Exists(dir)) {
                    dir += "_";
                }
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                if (AllImage) {
                    indexes = new int[album.List.Count];
                    for(var i = 0; i < indexes.Length; i++) {
                        indexes[i] = i;
                    }
                } 
                var max = Math.Min(indexes.Length, album.List.Count);

                StringBuilder sb = new StringBuilder(max);
                int maxW = 0;
                int maxH = 0;
                int maxPX = 0;
                int maxPY = 0;

                for (var i = 0; i < max; i++) {
                    if (indexes[i] < 0) {
                        continue;
                    }
                    var entity = album.List[indexes[i]];
                    sb.Append($"{entity.Location.X} {entity.Location.Y}\r\n");
                    if (entity.Width > maxW)
                    {
                        maxW = entity.Width;
                    }
                    if (entity.Height > maxH)
                    {
                        maxH = entity.Height;
                    }
                    if (entity.Location.X > maxPX)
                    {
                        maxPX = entity.Location.X;
                    }
                    if (entity.Location.Y > maxPY)
                    {
                        maxPY = entity.Location.Y;
                    }

                    var name = (Increment == -1 ? indexes[i] : Increment + i).ToString();
                    while (name.Length < Digit) {
                        name = string.Concat("0", name);
                    }
                    var path = $"{dir}{prefix}{name}.png"; //文件名格式:文件路径/贴图索引.png
                    var image = entity.Picture;
                    if (OnSaving != null) {
                        foreach (SpriteEffect action in OnSaving.GetInvocationList()) {
                            action.Invoke(entity, ref image);
                            image = image ?? entity.Picture;
                        }
                    }
                    var parent = System.IO.Path.GetDirectoryName(path);
                    image.Save(path); //保存贴图
                }
                //保存中心点
                var txtPath = $"{dir}{prefix}x.txt";
                try {
                    string dr = System.IO.Path.GetDirectoryName(txtPath);
                    if (!Directory.Exists(dr)) {
                        Directory.CreateDirectory(dr);
                    }
                    if (!File.Exists(txtPath)) {
                        FileStream fs = File.Create(txtPath);
                        StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                        sw.Write(sb.ToString());
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                } catch (Exception e) {
     
                }

                var whPath = $"{dir}{prefix}wh.txt";
                try
                {
                    string dr = System.IO.Path.GetDirectoryName(whPath);
                    if (!Directory.Exists(dr))
                    {
                        Directory.CreateDirectory(dr);
                    }
                    if (!File.Exists(whPath))
                    {
                        FileStream fs = File.Create(whPath);
                        StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                        sw.WriteLine(maxW + " " + maxH);
                        sw.WriteLine(maxPX + " " + maxPY);
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                }
                catch (Exception e)
                {

                }

            }
        }

        public bool CanUndo => false;

        public bool IsChanged => false;

        public string Name => "SaveImage";
    }
}