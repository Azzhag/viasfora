﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Winterdom.Viasfora {
  public static class AdornmentLayers {
    // Peek Definition uses this layer
    public const String InterLine = "Inter Line Adornment";
  }
  // new in VS2012/3
  public static class ViewRoles {
    public const String EmbeddedPeekTextView = "EMBEDDED_PEEK_TEXT_VIEW";
		public const String CodeDefinitionView = "CODEDEFINITION";
    public const String ToolTipView = "VIASFORA_TOOLTIP";
  }

  public static class ViewOptions {
    public const String HighlightCurrentLineOption = "Adornments/HighlightCurrentLine/Enable";
    public const String WordWrapStyleId = "TextView/WordWrapStyle";
    public const String ViewProhibitUserInput = "TextView/ProhibitUserInput";
  }

  public static class FontsAndColorsCategories {
    public const String ToolTipFontAndColorCategory = "A9A5637F-B2A8-422E-8FB5-DFB4625F0111";
  }
}
