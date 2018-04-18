﻿using FastColoredTextBoxNS;
using NIDE.ProjectTypes.MCPEModding;

namespace NIDE.Editors
{
    class JSCodeEditor : CodeEditor
    {
        public JSCodeEditor(string file) : base(file)
        {

        }

        public override bool Edit()
        {
            if (!base.Edit())
                return false;

            TextBox.Language = Language.JS;
            Focus();
            Update(TextBox.Range);
            return true;
        }

        public override void Focus()
        {
            Autocomplete.SetAutoompleteMenu(TextBox);
            Autocomplete.Enabled = true;
            Update(TextBox.Range);
        }

        public override void Update(Range range)
        {
            CodeAnalysisEngine.Update(this);
            ProgramData.MainForm.UpdateHighlighting(range);
        }
    }
}
