// HelloSource - a tiny ScriptHookDotNet test script, loaded straight from source by IVSDKScriptCompiler.
// Put this file in the GTA IV "scripts" folder. A few seconds after you are in the game it shows a short message.
using System;
using GTA;

public class HelloSource : Script
{
    private int shownAt = 0;
    private bool shown = false;

    public HelloSource()
    {
        Interval = 1000;
        this.Tick += new EventHandler(OnTick);
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (shown) return;
        if (Player == null || Player.Character == null || !Player.Character.Exists()) return;
        if (shownAt == 0) { shownAt = Environment.TickCount; return; }
        if (Environment.TickCount - shownAt < 6000) return;
        Game.DisplayText("HelloSource.net.cs: compiled from source and running", 5000);
        shown = true;
    }
}
