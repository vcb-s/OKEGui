using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;


namespace OKEGui.Utils
{
    class SystemMenu
    {
        #region Native methods

        private const int WM_SYSCOMMAND = 0x112;
        private const int MF_STRING = 0x0;
        private const int MF_SEPARATOR = 0x800;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool InsertMenu(IntPtr hMenu, int uPosition, int uFlags, int uIDNewItem, string lpNewItem);

        #endregion Native methods

        #region Private data

        private Window window;
        private IntPtr hSysMenu;
        private bool isHandleCreated;
        private int lastId;
        private List<Action> actions = new List<Action>();
        private List<CommandInfo> pendingCommands;

        #endregion Private data

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemMenu"/> class for the specified
        /// <see cref="Form"/>.
        /// </summary>
        /// <param name="form">The window for which the system menu is expanded.</param>
        public SystemMenu(Window window)
        {
            isHandleCreated = false;
            this.window = window;
            this.window.Loaded += OnHandleCreated;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Adds a command to the system menu.
        /// </summary>
        /// <param name="text">The displayed command text.</param>
        /// <param name="action">The action that is executed when the user clicks on the command.</param>
        /// <param name="separatorBeforeCommand">Indicates whether a separator is inserted before the command.</param>
        public void AddCommand(string text, Action action, bool separatorBeforeCommand)
        {
            var id = ++lastId;
            if (!isHandleCreated)
            {
                // The form is not yet created, queue the command for later addition
                if (pendingCommands == null)
                {
                    pendingCommands = new List<CommandInfo>();
                }
                pendingCommands.Add(new CommandInfo
                {
                    Id = id,
                    Text = text,
                    Action = action,
                    Separator = separatorBeforeCommand
                });
            }
            else
            {
                // The form is created, add the command now
                if (separatorBeforeCommand)
                {
                    AppendMenu(hSysMenu, MF_SEPARATOR, 0, "");
                }
                AppendMenu(hSysMenu, MF_STRING, id, text);
            }
            actions.Add(action);
        }

        /// <summary>
        /// Tests a window message for system menu commands and executes the associated action. This
        /// method must be called from within the Form's overridden WndProc method because it is not
        /// publicly accessible.
        /// </summary>
        /// <param name="msg">The window message to test.</param>
        public IntPtr HandleMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // This method is kept short and simple to allow inlining (verified) for improving
            // performance (unverified). It will be called for every single message that is sent to
            // the window.
            if (msg == WM_SYSCOMMAND)
            {
                OnSysCommandMessage(wParam, ref handled);
            }
            return IntPtr.Zero;
        }

        #endregion Public methods

        #region Private methods

        private void OnHandleCreated(object sender, EventArgs args)
        {
            isHandleCreated = true;
            window.Loaded -= OnHandleCreated;
            var hwnd = new WindowInteropHelper(this.window).Handle;
            hSysMenu = GetSystemMenu(hwnd, false);

            // Add all queued commands now
            if (pendingCommands != null)
            {
                foreach (var command in pendingCommands)
                {
                    if (command.Separator)
                    {
                        AppendMenu(hSysMenu, MF_SEPARATOR, 0, "");
                    }
                    AppendMenu(hSysMenu, MF_STRING, command.Id, command.Text);
                }
                pendingCommands = null;
            }
        }

        private void OnSysCommandMessage(IntPtr WParam, ref bool handled)
        {
            if ((long)WParam > 0 && (long)WParam <= lastId)
            {
                handled = true;
                actions[(int)WParam - 1]();
            }
        }

        #endregion Private methods

        #region Classes

        private class CommandInfo
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public Action Action { get; set; }
            public bool Separator { get; set; }
        }

        #endregion Classes
    }
}

