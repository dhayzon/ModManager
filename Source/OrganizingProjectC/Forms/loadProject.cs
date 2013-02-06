﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Data.SQLite;
using ModBuilder.Forms;

namespace ModBuilder
{
    public partial class loadProject : Form
    {
        public loadProject()
        {
            InitializeComponent();
        }

        public bool openProjDir(string dir)
        {
            // Check if the directory exists. Also should contain a package_info.xml.
            if (!Directory.Exists(dir) || !File.Exists(dir + "/Package/package-info.xml"))
                return false;

            // Start an instance of the mod editor.
            modEditor me = new modEditor();

            // Open up a new mod console.
            modConsole mc = new modConsole();

            // Try to parse the package_info.xml.
            XmlDocument doc = new XmlDocument();
            doc.Load(dir + "/Package/package-info.xml");

            // Post.
            mc.Message("package-info.xml has been detected. Parsing data...");

            // Read it!
            #region Boring XML parsing
            foreach (XmlNode l_packageNode in doc.LastChild.ChildNodes)
            {
                Console.WriteLine("Test node name: " + l_packageNode.Name);
                Console.WriteLine("Test value: " + l_packageNode.InnerText);
                switch (l_packageNode.Name)
                {
                    case "id":
                        string mid = l_packageNode.InnerText;
                        me.modID.Text = mid;

                        // Determine the mod author.
                        string[] pieces = mid.Split(':');
                        me.authorName.Text = pieces[0];
                        mc.Message("Found and inserted ID and author.");
                        break;
                    case "name":
                        me.modName.Text = l_packageNode.InnerText;
                        break;
                    case "version":
                        me.modVersion.Text = l_packageNode.InnerText;
                        break;
                    case "type":
                        if (l_packageNode.InnerText == "modification")
                            me.modType.SelectedItem = "Modification";
                        else
                            me.modType.SelectedItem = "Avatar pack";
                        break;
                    case "install":
                        me.modCompatibility.Text = l_packageNode.Attributes["for"].Value;
                        break;
                }
            }
            #endregion

            mc.Message("package-info.xml parsed. Reading files.");

            // Also load the readme.txt.
            if (File.Exists(dir + "/Package/readme.txt"))
            {
                me.modReadme.Text = File.ReadAllText(dir + "/Package/readme.txt");
                mc.Message("Read readme.txt");
            }

            if (File.Exists(dir + "/Package/install.php"))
            {
                me.customCodeInstall.Text = File.ReadAllText(dir + "/Package/install.php");
                mc.Message("Read install.php");
            }
            if (File.Exists(dir + "/Package/uninstall.php"))
            {
                me.customCodeUninstall.Text = File.ReadAllText(dir + "/Package/uninstall.php");
                mc.Message("Read uninstall.php");
            }
            if (File.Exists(dir + "/Package/installDatabase.php"))
            {
                me.installDatabaseCode.Text = File.ReadAllText(dir + "/Package/installDatabase.php");
                mc.Message("Read installDatabase.php");
            }
            if (File.Exists(dir + "/Package/uninstallDatabase.php"))
            {
                me.uninstallDatabaseCode.Text = File.ReadAllText(dir + "/Package/uninstallDatabase.php");
                mc.Message("Read uninstallDatabase.php");
            }

            if (!File.Exists(dir + "/data.sqlite"))
            {
                mc.Message("Unable to read data.sqlite; File not found.");
                MessageBox.Show("A required database was not found in your project. It will now be created.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                me.generateSQL(dir);
                mc.Message("A new database has been generated.");

                MessageBox.Show("A database file has been successfully created.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            mc.Message("Setting environment variables.");
            me.mc = mc;
            me.workingDirectory = dir;
            mc.Message("Working directory: " + dir);
            me.conn = new SQLiteConnection("Data Source=\"" + dir + "/data.sqlite\";Version=3;");
            me.conn.Open();
            me.hasConn = true;
            mc.Message("Database connection set.");

            mc.Message("Refreshing instruction and extraction trees.");
            me.refreshInstructionTree();
            me.refreshExtractionTree();
            mc.Message("Reloading settings.");
            me.reloadSettings();
            me.PopulateFileTree(dir, me.files.Nodes[0]);

            // Checks.
            if (!me.settings.ContainsKey("ignoreInstructions") || !me.settings.ContainsKey("autoGenerateModID") || !me.settings.ContainsKey("includeModManLine"))
            {
                MessageBox.Show("Your project does not contain all the required settings; please try to repair your project.", "Loading project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                me.conn.Close();
                return false;
            }

            mc.Message("Setting checkboxes according to settings.");
            if (me.settings["ignoreInstructions"] == "true")
                me.ignoreInstructions.Checked = true;

            if (me.settings["autoGenerateModID"] == "true")
                me.genPkgID.Checked = true;

            if (me.settings["includeModManLine"] == "true")
                me.includeModManLine.Checked = true;

            mc.Message("Opening Mod Editor");
            me.Show();

            return true;
        }
    }
}
