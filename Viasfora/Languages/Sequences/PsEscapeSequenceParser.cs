﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Winterdom.Viasfora.Util;

namespace Winterdom.Viasfora.Languages.Sequences {
  public class PsEscapeSequenceParser : IEscapeSequenceParser {
    private String text;
    private int start;
    public PsEscapeSequenceParser(String text) {
      this.text = text;
      // quotes are included, so start at 1
      this.start = 1;
      // single-quoted strings in powershell
      // don't support escape sequences
      if ( text.StartsWith("'") || text.StartsWith("@'") )
        this.start = text.Length;
    }
    public Span? Next() {
      while ( start < text.Length - 2 ) {
        if ( text[start] == '`' ) {
          var span = new Span(start, 2);
          start += 2;
          return span;
        }
        start++;
      }
      return null;
    }

  }
}
