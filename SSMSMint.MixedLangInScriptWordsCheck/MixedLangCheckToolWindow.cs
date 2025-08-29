using Microsoft.VisualStudio.Shell;
using SSMSMint.MixedLangInScriptWordsCheck.Views;
using System;
using System.Runtime.InteropServices;

namespace SSMSMint.MixedLangInScriptWordsCheck;

/// <summary>
/// This class implements the tool window exposed by this package and hosts a user control.
/// </summary>
/// <remarks>
/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
/// usually implemented by the package implementer.
/// <para>
/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
/// implementation of the IVsUIElementPane interface.
/// </para>
/// </remarks>
[Guid("5A979A59-3421-40EC-B1E0-6A28874A0C8D")]
public class MixedLangCheckToolWindow : ToolWindowPane
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MixedLangCheckToolWindow"/> class.
    /// </summary>
    public MixedLangCheckToolWindow() : base(null)
    {
        this.Caption = "Words with mixed language";

        // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
        // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
        // the object returned by the Content property.
        this.Content = new MixedLangCheckToolWindowControl();
    }
}