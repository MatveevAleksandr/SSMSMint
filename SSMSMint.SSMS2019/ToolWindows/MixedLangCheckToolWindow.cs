using SSMSMint.Core.UI.Interfaces;
using SSMSMint.Core.UI.View;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace SSMSMint.SSMS2019.ToolWindows;

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
[Guid("4B8CE332-A414-463A-928D-DFF0B84D0329")]
public class MixedLangCheckToolWindow : ToolWindowPane, IToolWindowCore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MixedLangCheckToolWindow"/> class.
    /// </summary>
    public MixedLangCheckToolWindow() : base(null)
    {
        this.Caption = "Слова с разными алфавитами";

        // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
        // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
        // the object returned by the Content property.
        this.Content = new MixedLangCheckControl();
    }

    public void Initialize(IToolWindowParams twParams)
    {
        (this.Content as MixedLangCheckControl).Initialize(twParams);
    }
}