﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Winterdom.Viasfora.Design;
using Winterdom.Viasfora.Text;

namespace Winterdom.Viasfora.Options {
  [Guid(Guids.IntellisenseOptions)]
  public class IntellisenseOptions : DialogPage {
    public override void SaveSettingsToStorage() {
      base.SaveSettingsToStorage();
      var settings = SettingsContext.GetSettings();
      settings.TextCompletionEnabled = TextCompletionEnabled;
      settings.TCCompleteDuringTyping = CompleteDuringTyping;
      settings.TCHandleCompleteWord = HandleCompleteWord;
      settings.Save();
    }
    public override void LoadSettingsFromStorage() {
      base.LoadSettingsFromStorage();
      var settings = SettingsContext.GetSettings();
      TextCompletionEnabled = settings.TextCompletionEnabled;
      CompleteDuringTyping = settings.TCCompleteDuringTyping;
      HandleCompleteWord = settings.TCHandleCompleteWord;
    }

    [LocDisplayName("Enable Plain-Text Completion")]
    [Description("Enables auto-completion based on the plain text of the current document")]
    [Category("Completion")]
    public bool TextCompletionEnabled { get; set; }

    [LocDisplayName("Complete During Typing")]
    [Description("Automatically complete words as text is typed")]
    [Category("Options")]
    public bool CompleteDuringTyping { get; set; }

    [LocDisplayName("Handle Complete Word")]
    [Description("Respond to the built-in Complete Word command in Visual Studio")]
    [Category("Options")]
    public bool HandleCompleteWord { get; set; }
  }
}