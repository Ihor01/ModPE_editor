﻿using NIDE.Editors;
using FastColoredTextBoxNS;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using NIDE.Highlighting;

namespace NIDE.ProjectTypes.MCPEModding.ZCore
{
    class IncludesEditor : CodeEditor, IEnumerable<AutocompleteItem>
    {
        private AutocompleteMenu autocomplete;

        public IncludesEditor(string file) : base(file) { }

        public override bool Edit()
        {
            if (!base.Edit())
                return false;
            autocomplete = new AutocompleteMenu(TextBox);
            autocomplete.Items.SetAutocompleteItems(this);
            autocomplete.MinFragmentLength = 1;
            TextBox.Language = Language.Custom;
            CodeAnalysisEngine.Stop();
            return true;
        }

        public override void Focus()
        {
            Autocomplete.Enabled = false;
        }

        public IEnumerator<AutocompleteItem> GetEnumerator()
        {
            List<AutocompleteItem> items = new List<AutocompleteItem>();
            string path = ProgramData.Project.ScriptsPath;
            List<string> files = Util.GetFileList(new DirectoryInfo(path)).Relative(path);
            foreach (string file in files)
            {
                if(file != ".includes")
                    yield return new AutocompleteItem(file.Replace('\\', '/'));
            }
        }

        public override void RefreshStyles(Highlighter highlighter)
        {
           
        }

        public override void Update(Range range) { }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}