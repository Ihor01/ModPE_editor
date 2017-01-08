using FastColoredTextBoxNS;
using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Ionic.Zip;
using Yahoo.Yui.Compressor;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace ModPE_editor
{
    public partial class fMain : Form
    {
        bool saved = true;
        XmlDocument xml;

        public fMain(string[] args)
        {
            InitializeComponent();
            CodeAnalysisEngine.Initialize(fctbMain);
            RegisterWorker.Load(this);
            fctbMain.Language = Language.JS;
            Autocomplete.SetAutoompleteMenu(fctbMain);
            fctbMain.HighlightingRangeType = HighlightingRangeType.VisibleRange;
            try
            {
                ModPe.LoadModPeData("modpescript_dump.txt");
                CoreEngine.LoadCoreEngineData("core.dump");
            }
            catch
            {
                MessageBox.Show("Unable to load ModPE data. Check if modpescript_dump.txt exists, availiable and correct");
            }
            if (args.Length > 0)
            {
                try
                {
                    ProgramData.File = args[0];
                    fctbMain.OpenFile(ProgramData.File, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to load ProgramData.File: " + ex.Message);
                }
            }
        }

        private void fctbMain_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (fctbMain.Language == Language.JS)
            {
                Highlighting.ResetStyles(e.ChangedRange);
                CodeAnalysisEngine.Update();
            }
            saved = false;
        }

        private void fMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            RegisterWorker.Save(this);
            e.Cancel = !BeforeClosingFile();
        }

        private void tvFolders_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            string nodeName = e.Node.Text;
            if (nodeName.IndexOf('.') != -1)
            {
                string[] splitted = nodeName.Split('.');
                string ext = splitted[splitted.Length - 1].ToLower();
                string path = e.Node.FullPath;
                if (ext == "png")
                {
                    fPngEditor editor = new fPngEditor(path);
                    editor.ShowDialog();
                }
                else if (ext == "js")
                {
                    if (BeforeClosingFile())
                        LoadJS(path);
                }
                else if (ext == "xml")
                {
                    fctbMain.Language = Language.XML;
                    if (BeforeClosingFile())
                        LoadXML(path);
                }
                else if (ext == "json")
                {
                    new fJsonItem(path).ShowDialog();

                }
            }
        }

        private void tsmiNewScript_Click(object sender, EventArgs e)
        {
            var dlgFileName = new fDialog();
            if (dlgFileName.ShowDialog() == DialogResult.OK)
            {
                string name = dlgFileName.FileName;
                if (!name.ToLower().Contains(".js"))
                    name = name + ".js";
                if (ProgramData.Mode == WorkMode.CORE_ENGINE)
                {
                    Util.CreateFile("dev\\" + name);
                    File.AppendAllLines(ProgramData.Folder + "\\dev\\.includes", new string[] { name });
                }
                else
                {
                    Util.CreateFile("script\\" + name);
                }
                LoadDiretories();
            }
        }

        private void tsmiNewTexture_Click(object sender, EventArgs e)
        {
            var dlgFileName = new fDialog(true);
            if (dlgFileName.ShowDialog() == DialogResult.OK)
            {
                Bitmap png = new Bitmap(16, 16);
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        png.SetPixel(i, j, Color.Transparent);
                    }
                }
                string name = dlgFileName.FileName;
                if (!name.ToLower().Contains(".png"))
                    name = name + ".png";
                string path = ProgramData.Folder + (ProgramData.Mode == WorkMode.MODPKG ? "\\images\\" : "\\assets\\") + (dlgFileName.Type == ImageType.ITEMS_OPAQUE ? "items-opaque" : "terrain-atlas");
                png.Save(path + "\\" + name);
                LoadDiretories();
            }
        }

        private void tsmiDeleteTexture_Click(object sender, EventArgs e)
        {
            string path = tvFolders.SelectedNode.FullPath;
            if (path.IndexOf('.') != -1)
            {
                File.Delete(path);
                LoadDiretories();

            }
        }

        private void tsmiCoreEngineDocs_Click(object sender, EventArgs e)
        {
            Process.Start("CoreEngine help.chm");
        }

        private void openProjectInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(ProgramData.Folder);
        }

        //connections
        public int TextViewWidth { get { return tvFolders.Width; } set { tvFolders.Width = value; }  }

        //util
        private void LoadDiretories()
        {
            tvFolders.Nodes.Clear();
            DirectoryRecursive(tvFolders.Nodes.Add(ProgramData.Folder), new DirectoryInfo(ProgramData.Folder));
            tvFolders.Nodes[0].Expand();
        }

        private void DirectoryRecursive(TreeNode node, DirectoryInfo dir)
        {
            try
            {
                var dirs = dir.GetDirectories();
                var files = dir.GetFiles();
                foreach (var subdir in dirs)
                    DirectoryRecursive(AddNode(node, subdir.Name), subdir);
                foreach (var file in files)
                    AddNode(node, file.Name);
            }
            catch
            {
            }
        }

        private TreeNode AddNode(TreeNode node, string text)
        {
            return node.Nodes.Add(text);
        }

        //textworking
        private void tsmiUndo_Click(object sender, EventArgs e)
        {
            fctbMain.Undo();
        }

        private void tsmiRedo_Click(object sender, EventArgs e)
        {
            fctbMain.Redo();
        }

        private void tsmiFind_Click(object sender, EventArgs e)
        {
            fctbMain.ShowFindDialog();
        }

        private void tsmiReplace_Click(object sender, EventArgs e)
        {
            fctbMain.ShowReplaceDialog();
        }

        private void ctsmiAutoIndent_Click(object sender, EventArgs e)
        {
            fctbMain.DoAutoIndent();
        }

        private void tsmiComment_Click(object sender, EventArgs e)
        {
            fctbMain.CommentSelected();
        }

        private void tsmiSelectAll_Click(object sender, EventArgs e)
        {
            fctbMain.SelectAll();
        }

        private void tsmiAutocompleteItems_Click(object sender, EventArgs e)
        {
            var itemsList = new fAutocompleteItems();
            itemsList.ShowDialog();
        }

        //fileworking
        private void tsmiOpenRecent_Click(object sender, EventArgs e)
        {
            fRecentItems items = new fRecentItems();
            if (items.ShowDialog() == DialogResult.OK)
                if (BeforeClosingFile() && LoadFile(fRecentItems.Path))
                {
                    saved = true;
                }
        }

        private void tsmiNewJS_Click(object sender, EventArgs e)
        {
            if (BeforeClosingFile() && CreateJS())
            {
                tvFolders.Visible = false;
                ProgramData.Mode = WorkMode.JAVASCRIPT;
            }
        }

        private void tsmiNewJSFromModel_Click(object sender, EventArgs e)
        {
            tsmiNewJS_Click(sender, e);
            LoadModel();
        }

        private void tsmiOpenJS_Click(object sender, EventArgs e)
        {
            if (BeforeClosingFile() && LoadJS())
            {
                tvFolders.Visible = false;
                ProgramData.Mode = WorkMode.JAVASCRIPT;
                saved = true;
            }
        }

        private void tsmiNewModpkg_Click(object sender, EventArgs e)
        {
            if (BeforeClosingFile() && CreateModpkg())
            {
                ProgramData.Mode = WorkMode.MODPKG;
            }
        }

        private void tsmiNewModpkgFromModel_Click(object sender, EventArgs e)
        {
            tsmiNewModpkg_Click(sender, e);
            LoadModel();
        }

        private void tsmiOpenModpkg_Click(object sender, EventArgs e)
        {
            if (BeforeClosingFile() && LoadModpkg())
            {
                ProgramData.Mode = WorkMode.MODPKG;
                saved = true;
            }
        }

        private void tsmiNewCoreEngine_Click(object sender, EventArgs e)
        {
            if (BeforeClosingFile() && CreateCoreEngine())
            {
                ProgramData.Mode = WorkMode.CORE_ENGINE;
            }
        }

        private void tsmiOpenCoreEngine_Click(object sender, EventArgs e)
        {
            if (BeforeClosingFile() && LoadCoreEngine())
            {
                ProgramData.Mode = WorkMode.CORE_ENGINE;
                saved = true;
            }
        }

        private void tsmiSave_Click(object sender, EventArgs e)
        {
            saved = Save();
        }

        private void tsmiSaveAs_Click(object sender, EventArgs e)
        {
            string _file = ProgramData.File, _folder = ProgramData.Folder;
            ProgramData.File = ""; ProgramData.Folder = "";
            saved = Save();
            if (!saved)
            {
                ProgramData.File = _file; ProgramData.Folder = _folder;
            }
        }


        private bool LoadModel()
        {
            dlgOpen.InitialDirectory = Directory.GetCurrentDirectory() + "\\models";
            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    fctbMain.OpenFile(dlgOpen.FileName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else return false;
        }

        private bool BeforeClosingFile()
        {
            RegisterWorker.AddRecent();
            if (saved || fctbMain.Text == "")
                return true;
            var result = MessageBox.Show("Do you want to save changes?", "Confirmation", MessageBoxButtons.YesNoCancel);
            if (result == DialogResult.Yes)
            {
                return Save();
            }
            else if (result == DialogResult.No)
            {
                return true;
            }
            else return false;
        }

        private bool InitJS()
        {
            fctbMain.Language = Language.JS;
            fctbMain.Refresh();
            return true;
        }

        private bool InitXML()
        {
            fctbMain.Language = Language.XML;
            tvFolders.Visible = false;
            return true;
        }

        private bool InitModpkg()
        {
            try
            {
                ProgramData.File = ProgramData.Folder + "\\script\\main.js";
                fctbMain.OpenFile(ProgramData.File, Encoding.UTF8);
                CodeAnalysisEngine.Update();
                tvFolders.Visible = true;
                LoadOrCreateXML();
                LoadDiretories();
                fctbMain.Language = Language.JS;
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to load .modpkg");
                return false;
            }
        }

        private bool InitCoreEngine()
        {
            try
            {
                ProgramData.File = ProgramData.Folder + "\\main.js";
                fctbMain.OpenFile(ProgramData.File, Encoding.UTF8);
                CodeAnalysisEngine.Update();
                tvFolders.Visible = true;
                LoadOrCreateXML();
                LoadDiretories();
                fctbMain.Language = Language.JS;
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to load CoreEngine project");
                return false;
            }
        }

        private bool LoadOrCreateXML()
        {
            try
            {
                xml = new XmlDocument();
                if (File.Exists(ProgramData.Folder + "\\project_info.xml"))
                {
                    xml.Load(ProgramData.Folder + "\\project_info.xml");
                    LoadAutocompleteItems();
                }
                else if (File.Exists(ProgramData.Folder + "\\projet_info.xml"))
                {
                    xml.Load(ProgramData.Folder + "\\projet_info.xml");
                    xml.Save(ProgramData.Folder + "\\project_info.xml");
                    File.Delete(ProgramData.Folder + "\\projet_info.xml");
                    LoadAutocompleteItems();
                }
                else
                {
                    xml.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><settings></settings>");
                    if (Directory.GetDirectories(ProgramData.Folder).Contains(ProgramData.Folder + "\\script"))
                    {
                        XmlElement el = xml.CreateElement("pack");
                        el.InnerText = "true";
                        xml.DocumentElement.AppendChild(el);
                    }
                    xml.Save(ProgramData.Folder + "\\project_info.xml");
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Cannot initialize xml");
                return false;
            }
        }

        private void LoadAutocompleteItems()
        {
            var items = xml.DocumentElement.GetElementsByTagName("UserAutocompleteItem");
            if (items.Count != 0)
            {
                List<string> list = new List<string>();
                foreach (var item in items)
                {
                    list.Add((item as XmlNode).InnerText);
                }
                Autocomplete.UserItems = list.ToArray();
            }
        }

        private bool CreateJS()
        {
            if (BeforeClosingFile())
            {
                ProgramData.File = "";
                fctbMain.Clear();
                return InitJS();
            }
            return false;
        }

        private bool CreateModpkg()
        {
            if (dlgFolder.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ProgramData.Folder = dlgFolder.SelectedPath;
                    Util.CreateDirectory("images\\items-opaque");
                    Util.CreateDirectory("images\\terrain-atlas");
                    Util.CreateDirectory("script");
                    Util.CreateFile("script\\main.js");
                    return InitModpkg();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Cannot create .modpkg");
                    return false;
                }
            }
            else return false;
        }

        private bool CreateCoreEngine()
        {
            if (dlgFolder.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ProgramData.Folder = dlgFolder.SelectedPath;
                    Util.CreateFile("main.js");
                    Util.CreateFile("launcher.js");
                    Util.CreateFile("mod.info");
                    Util.CreateFile("resources.zip");
                    Util.CreateDirectory("gui");
                    Util.CreateDirectory("dev");
                    Util.CreateDirectory("assets\\items-opaque");
                    Util.CreateDirectory("assets\\terrain-atlas");
                    Util.CreateFile("dev\\.includes");
                    return InitCoreEngine();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Cannot create CoreEngine project");
                    return false;
                }
            }
            else return false;
        }

        private bool LoadJS()
        {
            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                return LoadJS(dlgOpen.FileName);
            }
            else return false;
        }

        private bool LoadJS(string path)
        {
            try
            {
                ProgramData.File = path;
                fctbMain.OpenFile(ProgramData.File, Encoding.UTF8);
                CodeAnalysisEngine.Update();
                return InitJS();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to load ProgramData.File");
                return false;
            }
        }

        private bool LoadXML(string path)
        {
            try
            {
                ProgramData.File = path;
                fctbMain.OpenFile(ProgramData.File, Encoding.UTF8);
                return InitJS();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to load ProgramData.File");
                return false;
            }
        }

        private bool LoadModpkg()
        {
            if (dlgFolder.ShowDialog() == DialogResult.OK)
            {
                return LoadModpkg(dlgFolder.SelectedPath);
            }
            else return false;
        }

        private bool LoadModpkg(string path)
        {
            if (File.Exists(path + "\\script\\main.js"))
            {
                ProgramData.Folder = path;
                return InitModpkg();
            }
            else
            {
                MessageBox.Show("This ProgramData.Folder isn't a modpkg!");
                return false;
            }
        }

        private bool LoadCoreEngine()
        {
            if (dlgFolder.ShowDialog() == DialogResult.OK)
            {
                return LoadCoreEngine(dlgFolder.SelectedPath);
            }
            else return false;
        }

        private bool LoadCoreEngine(string path)
        {
            if (File.Exists(path + "\\main.js") &&
                File.Exists(path + "\\dev\\.includes") &&
                File.Exists(path + "\\launcher.js") &&
                File.Exists(path + "\\mod.info"))
            {
                ProgramData.Folder = path;
                return InitCoreEngine();
            }
            else
            {
                MessageBox.Show("This ProgramData.Folder isn't a CoreEngine project!");
                return false;
            }
        }

        private bool LoadFile(string path)
        {
            if (path.Split('\\').Last().Contains("."))
            {
                ProgramData.Mode = WorkMode.JAVASCRIPT;
                tvFolders.Visible = false;
                return LoadJS(path);
            }
            else if (Directory.GetDirectories(path).Contains(path + "\\script"))
            {
                ProgramData.Mode = WorkMode.MODPKG;
                tvFolders.Visible = true;
                return LoadModpkg(path);
            }
            else
            {
                ProgramData.Mode = WorkMode.CORE_ENGINE;
                tvFolders.Visible = true;
                return LoadCoreEngine(path);
            }
        }

        private bool SaveJS()
        {
            if (ProgramData.File == "")
            {
                if (dlgSave.ShowDialog() == DialogResult.OK)
                {
                    ProgramData.File = dlgSave.FileName;
                }
                else return false;
            }
            try
            {
                fctbMain.SaveToFile(ProgramData.File, Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to save JS");
                return false;
            }
        }

        private bool SaveModpkg()
        {
            if (ProgramData.Folder == "")
            {
                if (dlgFolder.ShowDialog() == DialogResult.OK)
                {
                    ProgramData.Folder = dlgFolder.SelectedPath;
                }
                else return false;
            }
            try
            {
                fctbMain.SaveToFile(ProgramData.File, Encoding.UTF8);
                if (xml.GetElementsByTagName("pack")[0].InnerText == "true")
                {
                    string compressed;
                    try
                    {
                        JavaScriptCompressor compressor = new JavaScriptCompressor();
                        compressed = compressor.Compress(fctbMain.Text);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "Error in your Javascript code found!");
                        return false;
                    }
                    File.WriteAllText(ProgramData.File, compressed, Encoding.UTF8);
                }
                using (ZipFile zip = new ZipFile())
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
                    zip.AddDirectoryByName("images");
                    zip.AddDirectoryByName("script");
                    zip.AddDirectory(ProgramData.Folder + "\\images", "images");
                    zip.AddDirectory(ProgramData.Folder + "\\script", "script");
                    zip.Save(ProgramData.Folder + "\\" + new DirectoryInfo(ProgramData.Folder).Name + ".modpkg");
                }
                fctbMain.SaveToFile(ProgramData.File, Encoding.UTF8);
                return SaveAutocompleteItems();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to save .modpkg");
                return false;
            }
        }

        private bool SaveAutocompleteItems()
        {
            try
            {
                if (Autocomplete.UserItems.Length != 0)
                {
                    foreach (var item in Autocomplete.UserItems)
                    {
                        if (!HasInnerText(xml.DocumentElement.GetElementsByTagName("UserAutocompleteItem"), item))
                        {
                            XmlElement el = xml.CreateElement("UserAutocompleteItem");
                            el.InnerText = item;
                            xml.DocumentElement.AppendChild(el);
                        }
                    }
                }
                xml.Save(ProgramData.Folder + "\\project_info.xml");
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to save UserAutocompleteItems");
                return false;
            }
        }

        private bool HasInnerText(XmlNodeList list, string text)
        {
            foreach (var node in list)
            {
                if ((node as XmlNode).InnerText == text)
                    return true;
            }
            return false;
        }

        private bool SaveCoreEngine()
        {
            try
            {
                fctbMain.SaveToFile(ProgramData.File, Encoding.UTF8);
                using (ZipFile zip = new ZipFile())
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
                    zip.AddDirectory(ProgramData.Folder + "\\assets");
                    zip.Save(ProgramData.Folder + "\\" + "resources.zip");
                }
                return SaveAutocompleteItems();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to save CoreEngine");
                return false;
            }
        }

        private bool Save()
        {
            if (ProgramData.Mode == WorkMode.MODPKG)
                return SaveModpkg();
            else if (ProgramData.Mode == WorkMode.JAVASCRIPT)
                return SaveJS();
            else if (ProgramData.Mode == WorkMode.CORE_ENGINE)
                return SaveCoreEngine();
            else return false;
        }
                
        //debugger
        private void tsmiRun_Click(object sender, EventArgs e)
        {
            if (fctbMain.Text != "")
                try
                {
                    JavaScriptCompressor compressor = new JavaScriptCompressor();
                    string compressed = compressor.Compress(fctbMain.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error in your Javascript code found!");
                }
        }

        //inserts
        private void tsmiNewItem_Click(object sender, EventArgs e)
        {
            if (ProgramData.Mode != WorkMode.MODPKG)
            {
                MessageBox.Show("This function is only for Modpkgs at the moment");
                return;
            }
            insertItemsEngine();
            fJsonItem form = new fJsonItem();
            if (form.ShowDialog() != DialogResult.Cancel)
            {
                fctbMain.AppendText("\nSetTileFromJson(\"" + fJsonItem.name + ".json\");");
                LoadDiretories();
            }

        }

        private void insertItemsEngine()
        {
            if (!fctbMain.Text.Contains("/*ItemsEngine. DO NOT CHANGE*/"))
            {
                try
                {
                    fctbMain.AppendText(File.ReadAllText("ItemsEngine.js"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cannot load ItemsEngine temlate");
                }
            }
        }

        private void craftRecipieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ProgramData.Mode == WorkMode.CORE_ENGINE)
            {
                MessageBox.Show("This function is only for pure ModPE at the moment");
                return;
            }
            var form = new fCraft();
            if (form.ShowDialog() == DialogResult.OK)
                fctbMain.AppendText("\n" + fCraft.recipie);
        }

        private void tsmiSettings_Click(object sender, EventArgs e)
        {
            new fSettings().ShowDialog();
            Highlighting.ResetStyles(fctbMain.Range);
        }
    }
}