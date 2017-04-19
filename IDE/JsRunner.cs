﻿using Noesis.Javascript;

namespace NIDE
{
    public class JsRunner
    {
        public delegate void Log(object iString);

        public JsRunner(string code)
        {
            log("Running code...");
            using (JavascriptContext context = new JavascriptContext())
            {
                Log print = log;
                context.SetParameter("print", print);
                context.SetParameter("clientMessage", print);
                try
                {
                    context.Run(code);
                }
                catch(JavascriptException e)
                {
                    log(e.Message + "\n");
                }
            }

        }

        private void log(object iString)
        {
            ProgramData.MainForm?.Log("JsRunner", iString.ToString());
        }
    }
}