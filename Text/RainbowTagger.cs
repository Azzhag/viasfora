﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Winterdom.Viasfora.Text {

  class RainbowTagger : ITagger<ClassificationTag>, IDisposable {
    private ITextBuffer theBuffer;
    private ITextView theView;
    private ITagAggregator<IClassificationTag> aggregator;
    private ClassificationTag[] rainbowTags;
    private Dictionary<char, char> braceList = new Dictionary<char, char>();
    private HashSet<String> tagsToIgnore = new HashSet<String>();
    private const String braceChars = "(){}[]";
    private const int MAX_DEPTH = 4;

#pragma warning disable 67
    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

    internal RainbowTagger(
          ITextBuffer buffer, ITextView textView,
          IClassificationTypeRegistryService registry,
          ITagAggregator<IClassificationTag> aggregator) {
      this.theView = textView;
      this.theBuffer = buffer;
      this.aggregator = aggregator;
      rainbowTags = new ClassificationTag[MAX_DEPTH];
      for ( int i = 0; i < MAX_DEPTH; i++ ) {
        rainbowTags[i] = new ClassificationTag(
          registry.GetClassificationType(Constants.RAINBOW + (i + 1)));
      }
      for ( int i = 0; i < braceChars.Length; i += 2 ) {
        braceList.Add(braceChars[i], braceChars[i + 1]);
      }

      tagsToIgnore.Add("comment");
      tagsToIgnore.Add("string");
      //tagsToIgnore.Add("keyword");
      //tagsToIgnore.Add("identifier");

      this.theBuffer.Changed += BufferChanged;
      this.theView.LayoutChanged += ViewLayoutChanged;
      VsfSettings.SettingsUpdated += this.OnSettingsUpdated;
    }

    public void Dispose() {
      if ( theBuffer != null ) {
        VsfSettings.SettingsUpdated -= OnSettingsUpdated;
        theBuffer = null;
      }
    }

    public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
      if ( !VsfSettings.RainbowTagsEnabled ) yield break;
      if ( spans.Count == 0 ) {
        yield break;
      }
      ITextSnapshot snapshot = spans[0].Snapshot;
      if ( !IsSupported(snapshot.ContentType) ) {
        yield break;
      }
      SnapshotPoint startPoint = new SnapshotPoint(snapshot, 0);
      //SnapshotPoint startPoint = new SnapshotPoint(snapshot, spans[0].Start);
      foreach ( var tagSpan in LookForMatchingPairs(startPoint) ) {
        yield return tagSpan;
      }
    }

    class Pair {
      public char Brace { get; set; }
      public int Depth { get; set; }
      public int Open { get; set; }
      public int Close { get; set; }
    }

    private IEnumerable<ITagSpan<ClassificationTag>> LookForMatchingPairs(SnapshotPoint startPoint) {
      Stack<Pair> pairs = new Stack<Pair>();
      ITextSnapshot snapshot = startPoint.Snapshot;

      FindBracePairs(startPoint, pairs);

      foreach ( var p in pairs ) {
        var tag = this.rainbowTags[p.Depth % MAX_DEPTH];
        var span = new SnapshotSpan(snapshot, p.Open, 1);
        yield return new TagSpan<ClassificationTag>(span, tag);
        if ( p.Close >= 0 ) {
          span = new SnapshotSpan(snapshot, p.Close, 1);
          yield return new TagSpan<ClassificationTag>(span, tag);
        }
      }
    }

    private void FindBracePairs(SnapshotPoint startPoint, Stack<Pair> pairs)  {
      ITextSnapshot snapshot = startPoint.Snapshot;
      var toIgnore = FindTagSpansToIgnore(startPoint).ToList();
      int startLine = snapshot.GetLineNumberFromPosition(startPoint.Position);

      int depth = 0;
      for ( int lineNr = startLine; lineNr < snapshot.LineCount; lineNr++ ) {
        ITextSnapshotLine line = snapshot.GetLineFromLineNumber(lineNr);
        String text = line.GetText();
        for ( int i = 0; i < line.Length; i++ ) {
          char ch = text[i];
          if ( ShouldIgnore(line.Start + i, toIgnore) ) {
            continue;
          }
          if ( IsOpeningBrace(ch) ) {
            pairs.Push(new Pair {
              Brace = ch, Depth = depth,
              Open = line.Start + i, Close = -1
            });
            depth++;
          } else if ( IsClosingBrace(ch) ) {
            if ( MatchBrace(pairs, ch, line.Start + i) )
              depth--;
          }
        }
      }
    }

    private bool ShouldIgnore(int position, List<SnapshotSpan> toIgnore) {
      foreach ( var span in toIgnore ) {
        if ( span.Contains(position) ) {
          return true;
        }
      }
      return false;
    }

    private bool MatchBrace(Stack<Pair> pairs, char ch, int pos) {
      foreach ( var p in pairs ) {
        if ( p.Close < 0 && braceList[p.Brace] == ch ) {
          p.Close = pos;
          return true;
        }
      }
      return false;
    }

    private bool IsClosingBrace(char ch) {
      return braceList.Values.Contains(ch);
    }

    private bool IsOpeningBrace(char ch) {
      return braceList.ContainsKey(ch);
    }

    private IEnumerable<SnapshotSpan> FindTagSpansToIgnore(SnapshotPoint startPoint) {
      ITextSnapshot snapshot = startPoint.Snapshot;
      SnapshotSpan totalSpan = new SnapshotSpan(startPoint, snapshot.Length - startPoint.Position);

      var query = from mappingTagSpan in aggregator.GetTags(totalSpan)
                  let name = mappingTagSpan.Tag.ClassificationType.Classification.ToLower()
                  where tagsToIgnore.Contains(name)
                  select mappingTagSpan.Span;
      var classifiedSpans = from mappedSpan in query
                            let cs = mappedSpan.GetSpans(snapshot)
                            where cs.Count > 0
                            select cs[0];
      return classifiedSpans;
    }

    void OnSettingsUpdated(object sender, EventArgs e) {
      UpdateTags(theBuffer.CurrentSnapshot, 0);
    }

    private void BufferChanged(object sender, TextContentChangedEventArgs e) {
      UpdateTags(e.After, e.Changes[0].NewSpan.Start);
    }
    private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
      if ( e.NewSnapshot != e.OldSnapshot ) {
        UpdateTags(e.NewSnapshot, 0);
      }
    }

    private void UpdateTags(ITextSnapshot snapshot, int startPosition) {
      var tempEvent = TagsChanged;
      if ( tempEvent != null ) {
        tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, startPosition,
            snapshot.Length - startPosition)));
      }
    }

    private bool IsSupported(IContentType contentType) {
      return contentType.IsOfType(CSharp.ContentType)
          || contentType.IsOfType(Cpp.ContentType)
          || contentType.IsOfType(JScript.ContentType)
          || contentType.IsOfType(JScript.ContentTypeVS2012);
    }
  }

}