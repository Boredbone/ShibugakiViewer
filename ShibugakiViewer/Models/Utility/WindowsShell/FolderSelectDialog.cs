using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;
using System.Collections;


namespace ShibugakiViewer.Models.Utility.WindowsShell
{

    public class FolderSelectDialog : IDisposable
    {

        private FolderBrowserDialog Dialog { get; }
        public string DefaultDirectory { get; set; }/*
        {
            get { return this.Dialog.DefaultDirectory; }
            set { this.Dialog.DefaultDirectory = value; }
        }*/

        //public string SelectedPath => this.Dialog.SelectedPath;

        public List<string> SelectedItems { get; } = new List<string>();

        public FolderSelectDialog()
        {
            var dialog = new FolderBrowserDialog()
            {/*
                IsFolderPicker = true,
                AllowNonFileSystemItems = true,
                EnsurePathExists = true,
                EnsureFileExists = false,*/
            };
            this.Dialog = dialog;
        }

        public bool? ShowDialog()
        {
            var window = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .SingleOrDefault(w => w.IsActive);

            //this.Dialog.InitialDirectory = this.DefaultDirectory;
            if (!string.IsNullOrWhiteSpace(this.DefaultDirectory))
            {
                this.Dialog.SelectedPath = this.DefaultDirectory;
            }

            var result = (window == null) ? this.Dialog.ShowDialog() : this.Dialog.ShowDialog(window);

            switch (result)
            {
                case DialogResult.OK:
                    break;
                case DialogResult.Cancel:
                    return false;
                default:
                    return null;
            }

            this.SelectedItems.Clear();

            foreach (var item in this.Dialog.SelectedItems)
            {
                if (item.Attribute.HasFlag(ShellFileGetAttributesOptions.Storage))
                {
                    if ((".library-ms").Equals(Path.GetExtension(item.Path)))
                    {
                        var libraryPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            @"Microsoft\Windows\Libraries\");
                        var libraryName = Path.GetFileNameWithoutExtension
                            (item.Path.Split(Path.DirectorySeparatorChar).Last());

                        using (var shellLibrary = ShellLibrary.Load(libraryName, libraryPath, true))
                        {
                            foreach (var folder in shellLibrary)
                            {
                                this.SelectedItems.Add(folder.Path);
                            }
                        }
                    }
                    else
                    {

                        this.SelectedItems.Add(item.Path);
                    }

                }
            }
            return true;
        }

        public void Dispose()
        {
            //this.Dialog.Dispose();
        }
    }



    /// <summary>
    /// FolderBrowserDialog クラスは、フォルダーを選択する機能を提供するクラスです。
    /// <para>
    /// <see cref="Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialog"/> クラスを利用したフォルダーの選択に近い機能を提供します。
    /// </para>
    /// </summary>
    public class FolderBrowserDialog
    {

        #region Properties

        internal class FolderItem
        {
            public string Path { get; set; }
            public ShellFileGetAttributesOptions Attribute { get; set; }
        }

        internal List<FolderItem> SelectedItems { get; set; }

        /// <summary>
        /// ユーザーによって選択されたフォルダーのパスを取得または設定します。
        /// </summary>
        public string SelectedPath { get; set; }

        /// <summary>
        /// ダイアログ上に表示されるタイトルのテキストを取得または設定します。
        /// </summary>
        public string Title { get; set; }

        #endregion

        #region Initializes

        /// <summary>
        /// <see cref="FolderBrowserDialog"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public FolderBrowserDialog() { }

        #endregion


        #region Public Methods

        public DialogResult ShowDialog()
        {
            return ShowDialog(IntPtr.Zero);
        }

        public DialogResult ShowDialog(System.Windows.Window owner)
        {
            if (owner == null)
            {
                return ShowDialog(IntPtr.Zero);
                //throw new ArgumentNullException("指定したウィンドウは null です。オーナーを正しく設定できません。");
            }

            var handle = new System.Windows.Interop.WindowInteropHelper(owner).Handle;

            return ShowDialog(handle);
        }

        public DialogResult ShowDialog(IntPtr owner)
        {
            var dialog = new FileOpenDialogRCW() as IFileOpenDialog;

            try
            {
                //dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
                dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_ALLNONSTORAGEITEMS);

                if (!string.IsNullOrEmpty(SelectedPath))
                {
                    IntPtr idl = IntPtr.Zero; // path の intptr
                    uint attributes = 0;

                    if (ShellNativeMethods.SHILCreateFromPath(SelectedPath, out idl, ref attributes) == 0)
                    {
                        if (ShellNativeMethods.SHCreateShellItem(IntPtr.Zero, IntPtr.Zero, idl, out var item1) == 0)
                        {
                            dialog.SetFolder(item1);
                        }

                        if (idl != IntPtr.Zero)
                        {
                            Marshal.FreeCoTaskMem(idl);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Title))
                {
                    dialog.SetTitle(Title);
                }

                var hr = dialog.Show(owner);

                // 選択のキャンセルまたは例外
                if (hr == HResult.Canceled) return DialogResult.Cancel;
                if (hr != 0) return DialogResult.Abort;

#if true

                dialog.GetResults(out IShellItemArray resultsArray);
                resultsArray.GetCount(out uint count);
                var names = new List<string>();
                //names.Clear();

                this.SelectedItems = new List<FolderItem>();

                for (int i = 0; i < count; i++)
                {
                    var shellItem = GetShellItemAt(resultsArray, i);
                    var path = ShellItemHelper.GetParsingName(shellItem);

                    shellItem.GetAttributes(
                        (ShellFileGetAttributesOptions)0x7fff_ffff,
                        out var attr);

                    var di = new FolderItem()
                    {
                        Path = path,
                        Attribute = attr,
                    };
                    this.SelectedItems.Add(di);

                    names.Add(path);
                }
                SelectedPath = string.Join(",\n", this.SelectedItems.Select(x => $"{x.Path}[{x.Attribute}]"));
#else
                dialog.GetResult(out item);
                if (item != null)
                {
                    item.GetDisplayName(
                        ShellItemDesignNameOptions.FileSystemPath,
                        out selectedPath);
                    SelectedPath = selectedPath;
                }
                else
                {
                    return DialogResult.Abort;
                }
#endif

                return DialogResult.OK;
            }
            finally
            {
                Marshal.FinalReleaseComObject(dialog);
            }
        }

        internal static IShellItem GetShellItemAt(IShellItemArray array, int i)
        {
            IShellItem result;
            uint index = (uint)i;
            array.GetItemAt(index, out result);
            return result;
        }

        #endregion
    }


    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7
    }







    public enum ShellItemDesignNameOptions : uint
    {
        Normal = 0x00000000,           // SIGDN_NORMAL
        ParentRelativeParsing = 0x80018001,   // SIGDN_INFOLDER | SIGDN_FORPARSING
        DesktopAbsoluteParsing = 0x80028000,  // SIGDN_FORPARSING
        ParentRelativeEditing = 0x80031001,   // SIGDN_INFOLDER | SIGDN_FOREDITING
        DesktopAbsoluteEditing = 0x8004c000,  // SIGDN_FORPARSING | SIGDN_FORADDRESSBAR
        FileSystemPath = 0x80058000,             // SIGDN_FORPARSING
        Url = 0x80068000,                     // SIGDN_FORPARSING
        ParentRelativeForAddressBar = 0x8007c001,     // SIGDN_INFOLDER | SIGDN_FORPARSING | SIGDN_FORADDRESSBAR
        ParentRelative = 0x80080001           // SIGDN_INFOLDER
    }


    /// <summary>
    /// HRESULT Wrapper    
    /// </summary>    
    public enum HResult
    {
        /// <summary>     
        /// S_OK          
        /// </summary>    
        Ok = 0x0000,

        /// <summary>
        /// S_FALSE
        /// </summary>        
        False = 0x0001,

        /// <summary>
        /// E_INVALIDARG
        /// </summary>
        InvalidArguments = unchecked((int)0x80070057),

        /// <summary>
        /// E_OUTOFMEMORY
        /// </summary>
        OutOfMemory = unchecked((int)0x8007000E),

        /// <summary>
        /// E_NOINTERFACE
        /// </summary>
        NoInterface = unchecked((int)0x80004002),

        /// <summary>
        /// E_FAIL
        /// </summary>
        Fail = unchecked((int)0x80004005),

        /// <summary>
        /// E_ELEMENTNOTFOUND
        /// </summary>
        ElementNotFound = unchecked((int)0x80070490),

        /// <summary>
        /// TYPE_E_ELEMENTNOTFOUND
        /// </summary>
        TypeElementNotFound = unchecked((int)0x8002802B),

        /// <summary>
        /// NO_OBJECT
        /// </summary>
        NoObject = unchecked((int)0x800401E5),

        /// <summary>
        /// Win32 Error code: ERROR_CANCELLED
        /// </summary>
        Win32ErrorCanceled = 1223,

        /// <summary>
        /// ERROR_CANCELLED
        /// </summary>
        Canceled = unchecked((int)0x800704C7),

        /// <summary>
        /// The requested resource is in use
        /// </summary>
        ResourceInUse = unchecked((int)0x800700AA),

        /// <summary>
        /// The requested resources is read-only.
        /// </summary>
        AccessDenied = unchecked((int)0x80030005)
    }


    /// <summary>
    /// Indicate flags that modify the property store object retrieved by methods 
    /// that create a property store, such as IShellItem2::GetPropertyStore or 
    /// IPropertyStoreFactory::GetPropertyStore.
    /// </summary>
    [Flags]
    internal enum GetPropertyStoreOptions
    {
        /// <summary>
        /// Meaning to a calling process: Return a read-only property store that contains all 
        /// properties. Slow items (offline files) are not opened. 
        /// Combination with other flags: Can be overridden by other flags.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Meaning to a calling process: Include only properties directly from the property
        /// handler, which opens the file on the disk, network, or device. Meaning to a file 
        /// folder: Only include properties directly from the handler.
        /// 
        /// Meaning to other folders: When delegating to a file folder, pass this flag on 
        /// to the file folder; do not do any multiplexing (MUX). When not delegating to a 
        /// file folder, ignore this flag instead of returning a failure code.
        /// 
        /// Combination with other flags: Cannot be combined with GPS_TEMPORARY, 
        /// GPS_FASTPROPERTIESONLY, or GPS_BESTEFFORT.
        /// </summary>
        HandlePropertiesOnly = 0x1,

        /// <summary>
        /// Meaning to a calling process: Can write properties to the item. 
        /// Note: The store may contain fewer properties than a read-only store. 
        /// 
        /// Meaning to a file folder: ReadWrite.
        /// 
        /// Meaning to other folders: ReadWrite. Note: When using default MUX, 
        /// return a single unmultiplexed store because the default MUX does not support ReadWrite.
        /// 
        /// Combination with other flags: Cannot be combined with GPS_TEMPORARY, GPS_FASTPROPERTIESONLY, 
        /// GPS_BESTEFFORT, or GPS_DELAYCREATION. Implies GPS_HANDLERPROPERTIESONLY.
        /// </summary>
        ReadWrite = 0x2,

        /// <summary>
        /// Meaning to a calling process: Provides a writable store, with no initial properties, 
        /// that exists for the lifetime of the Shell item instance; basically, a property bag 
        /// attached to the item instance. 
        /// 
        /// Meaning to a file folder: Not applicable. Handled by the Shell item.
        /// 
        /// Meaning to other folders: Not applicable. Handled by the Shell item.
        /// 
        /// Combination with other flags: Cannot be combined with any other flag. Implies GPS_READWRITE
        /// </summary>
        Temporary = 0x4,

        /// <summary>
        /// Meaning to a calling process: Provides a store that does not involve reading from the 
        /// disk or network. Note: Some values may be different, or missing, compared to a store 
        /// without this flag. 
        /// 
        /// Meaning to a file folder: Include the "innate" and "fallback" stores only. Do not load the handler.
        /// 
        /// Meaning to other folders: Include only properties that are available in memory or can 
        /// be computed very quickly (no properties from disk, network, or peripheral IO devices). 
        /// This is normally only data sources from the IDLIST. When delegating to other folders, pass this flag on to them.
        /// 
        /// Combination with other flags: Cannot be combined with GPS_TEMPORARY, GPS_READWRITE, 
        /// GPS_HANDLERPROPERTIESONLY, or GPS_DELAYCREATION.
        /// </summary>
        FastPropertiesOnly = 0x8,

        /// <summary>
        /// Meaning to a calling process: Open a slow item (offline file) if necessary. 
        /// Meaning to a file folder: Retrieve a file from offline storage, if necessary. 
        /// Note: Without this flag, the handler is not created for offline files.
        /// 
        /// Meaning to other folders: Do not return any properties that are very slow.
        /// 
        /// Combination with other flags: Cannot be combined with GPS_TEMPORARY or GPS_FASTPROPERTIESONLY.
        /// </summary>
        OpensLowItem = 0x10,

        /// <summary>
        /// Meaning to a calling process: Delay memory-intensive operations, such as file access, until 
        /// a property is requested that requires such access. 
        /// 
        /// Meaning to a file folder: Do not create the handler until needed; for example, either 
        /// GetCount/GetAt or GetValue, where the innate store does not satisfy the request. 
        /// Note: GetValue might fail due to file access problems.
        /// 
        /// Meaning to other folders: If the folder has memory-intensive properties, such as 
        /// delegating to a file folder or network access, it can optimize performance by 
        /// supporting IDelayedPropertyStoreFactory and splitting up its properties into a 
        /// fast and a slow store. It can then use delayed MUX to recombine them.
        /// 
        /// Combination with other flags: Cannot be combined with GPS_TEMPORARY or 
        /// GPS_READWRITE
        /// </summary>
        DelayCreation = 0x20,

        /// <summary>
        /// Meaning to a calling process: Succeed at getting the store, even if some 
        /// properties are not returned. Note: Some values may be different, or missing,
        /// compared to a store without this flag. 
        /// 
        /// Meaning to a file folder: Succeed and return a store, even if the handler or 
        /// innate store has an error during creation. Only fail if substores fail.
        /// 
        /// Meaning to other folders: Succeed on getting the store, even if some properties 
        /// are not returned.
        /// 
        /// Combination with other flags: Cannot be combined with GPS_TEMPORARY, 
        /// GPS_READWRITE, or GPS_HANDLERPROPERTIESONLY.
        /// </summary>
        BestEffort = 0x40,

        /// <summary>
        /// Mask for valid GETPROPERTYSTOREFLAGS values.
        /// </summary>
        MaskValid = 0xff,
    }

    internal enum ShellItemAttributeOptions
    {
        // if multiple items and the attirbutes together.
        And = 0x00000001,
        // if multiple items or the attributes together.
        Or = 0x00000002,
        // Call GetAttributes directly on the 
        // ShellFolder for multiple attributes.
        AppCompat = 0x00000003,

        // A mask for SIATTRIBFLAGS_AND, SIATTRIBFLAGS_OR, and SIATTRIBFLAGS_APPCOMPAT. Callers normally do not use this value.
        Mask = 0x00000003,

        // Windows 7 and later. Examine all items in the array to compute the attributes. 
        // Note that this can result in poor performance over large arrays and therefore it 
        // should be used only when needed. Cases in which you pass this flag should be extremely rare.
        AllItems = 0x00004000
    }


    [Flags]
    internal enum ShellFileGetAttributesOptions
    {
        /// <summary>
        /// The specified items can be copied.
        /// </summary>
        CanCopy = 0x00000001,

        /// <summary>
        /// The specified items can be moved.
        /// </summary>
        CanMove = 0x00000002,

        /// <summary>
        /// Shortcuts can be created for the specified items. This flag has the same value as DROPEFFECT. 
        /// The normal use of this flag is to add a Create Shortcut item to the shortcut menu that is displayed 
        /// during drag-and-drop operations. However, SFGAO_CANLINK also adds a Create Shortcut item to the Microsoft 
        /// Windows Explorer's File menu and to normal shortcut menus. 
        /// If this item is selected, your application's IContextMenu::InvokeCommand is invoked with the lpVerb 
        /// member of the CMINVOKECOMMANDINFO structure set to "link." Your application is responsible for creating the link.
        /// </summary>
        CanLink = 0x00000004,

        /// <summary>
        /// The specified items can be bound to an IStorage interface through IShellFolder::BindToObject.
        /// </summary>
        Storage = 0x00000008,

        /// <summary>
        /// The specified items can be renamed.
        /// </summary>
        CanRename = 0x00000010,

        /// <summary>
        /// The specified items can be deleted.
        /// </summary>
        CanDelete = 0x00000020,

        /// <summary>
        /// The specified items have property sheets.
        /// </summary>
        HasPropertySheet = 0x00000040,

        /// <summary>
        /// The specified items are drop targets.
        /// </summary>
        DropTarget = 0x00000100,

        /// <summary>
        /// This flag is a mask for the capability flags.
        /// </summary>
        CapabilityMask = 0x00000177,

        /// <summary>
        /// Windows 7 and later. The specified items are system items.
        /// </summary>
        System = 0x00001000,

        /// <summary>
        /// The specified items are encrypted.
        /// </summary>
        Encrypted = 0x00002000,

        /// <summary>
        /// Indicates that accessing the object = through IStream or other storage interfaces, 
        /// is a slow operation. 
        /// Applications should avoid accessing items flagged with SFGAO_ISSLOW.
        /// </summary>
        IsSlow = 0x00004000,

        /// <summary>
        /// The specified items are ghosted icons.
        /// </summary>
        Ghosted = 0x00008000,

        /// <summary>
        /// The specified items are shortcuts.
        /// </summary>
        Link = 0x00010000,

        /// <summary>
        /// The specified folder objects are shared.
        /// </summary>    
        Share = 0x00020000,

        /// <summary>
        /// The specified items are read-only. In the case of folders, this means 
        /// that new items cannot be created in those folders.
        /// </summary>
        ReadOnly = 0x00040000,

        /// <summary>
        /// The item is hidden and should not be displayed unless the 
        /// Show hidden files and folders option is enabled in Folder Settings.
        /// </summary>
        Hidden = 0x00080000,

        /// <summary>
        /// This flag is a mask for the display attributes.
        /// </summary>
        DisplayAttributeMask = 0x000FC000,

        /// <summary>
        /// The specified folders contain one or more file system folders.
        /// </summary>
        FileSystemAncestor = 0x10000000,

        /// <summary>
        /// The specified items are folders.
        /// </summary>
        Folder = 0x20000000,

        /// <summary>
        /// The specified folders or file objects are part of the file system 
        /// that is, they are files, directories, or root directories).
        /// </summary>
        FileSystem = 0x40000000,

        /// <summary>
        /// The specified folders have subfolders = and are, therefore, 
        /// expandable in the left pane of Windows Explorer).
        /// </summary>
        HasSubFolder = unchecked((int)0x80000000),

        /// <summary>
        /// This flag is a mask for the contents attributes.
        /// </summary>
        ContentsMask = unchecked((int)0x80000000),

        /// <summary>
        /// When specified as input, SFGAO_VALIDATE instructs the folder to validate that the items 
        /// pointed to by the contents of apidl exist. If one or more of those items do not exist, 
        /// IShellFolder::GetAttributesOf returns a failure code. 
        /// When used with the file system folder, SFGAO_VALIDATE instructs the folder to discard cached 
        /// properties retrieved by clients of IShellFolder2::GetDetailsEx that may 
        /// have accumulated for the specified items.
        /// </summary>
        Validate = 0x01000000,

        /// <summary>
        /// The specified items are on removable media or are themselves removable devices.
        /// </summary>
        Removable = 0x02000000,

        /// <summary>
        /// The specified items are compressed.
        /// </summary>
        Compressed = 0x04000000,

        /// <summary>
        /// The specified items can be browsed in place.
        /// </summary>
        Browsable = 0x08000000,

        /// <summary>
        /// The items are nonenumerated items.
        /// </summary>
        Nonenumerated = 0x00100000,

        /// <summary>
        /// The objects contain new content.
        /// </summary>
        NewContent = 0x00200000,

        /// <summary>
        /// It is possible to create monikers for the specified file objects or folders.
        /// </summary>
        CanMoniker = 0x00400000,

        /// <summary>
        /// Not supported.
        /// </summary>
        HasStorage = 0x00400000,

        /// <summary>
        /// Indicates that the item has a stream associated with it that can be accessed 
        /// by a call to IShellFolder::BindToObject with IID_IStream in the riid parameter.
        /// </summary>
        Stream = 0x00400000,

        /// <summary>
        /// Children of this item are accessible through IStream or IStorage. 
        /// Those children are flagged with SFGAO_STORAGE or SFGAO_STREAM.
        /// </summary>
        StorageAncestor = 0x00800000,

        /// <summary>
        /// This flag is a mask for the storage capability attributes.
        /// </summary>
        StorageCapabilityMask = 0x70C50008,

        /// <summary>
        /// Mask used by PKEY_SFGAOFlags to remove certain values that are considered 
        /// to cause slow calculations or lack context. 
        /// Equal to SFGAO_VALIDATE | SFGAO_ISSLOW | SFGAO_HASSUBFOLDER.
        /// </summary>
        PkeyMask = unchecked((int)0x81044000),
    }

    /// <summary>
    /// The STGM constants are flags that indicate 
    /// conditions for creating and deleting the object and access modes 
    /// for the object. 
    /// 
    /// You can combine these flags, but you can only choose one flag 
    /// from each group of related flags. Typically one flag from each 
    /// of the access and sharing groups must be specified for all 
    /// functions and methods which use these constants. 
    /// </summary>
    [Flags]
    public enum AccessModes
    {
        /// <summary>
        /// Indicates that, in direct mode, each change to a storage 
        /// or stream element is written as it occurs.
        /// </summary>
        Direct = 0x00000000,

        /// <summary>
        /// Indicates that, in transacted mode, changes are buffered 
        /// and written only if an explicit commit operation is called. 
        /// </summary>
        Transacted = 0x00010000,

        /// <summary>
        /// Provides a faster implementation of a compound file 
        /// in a limited, but frequently used, case. 
        /// </summary>
        Simple = 0x08000000,

        /// <summary>
        /// Indicates that the object is read-only, 
        /// meaning that modifications cannot be made.
        /// </summary>
        Read = 0x00000000,

        /// <summary>
        /// Enables you to save changes to the object, 
        /// but does not permit access to its data. 
        /// </summary>
        Write = 0x00000001,

        /// <summary>
        /// Enables access and modification of object data.
        /// </summary>
        ReadWrite = 0x00000002,

        /// <summary>
        /// Specifies that subsequent openings of the object are 
        /// not denied read or write access. 
        /// </summary>
        ShareDenyNone = 0x00000040,

        /// <summary>
        /// Prevents others from subsequently opening the object in Read mode. 
        /// </summary>
        ShareDenyRead = 0x00000030,

        /// <summary>
        /// Prevents others from subsequently opening the object 
        /// for Write or ReadWrite access.
        /// </summary>
        ShareDenyWrite = 0x00000020,

        /// <summary>
        /// Prevents others from subsequently opening the object in any mode. 
        /// </summary>
        ShareExclusive = 0x00000010,

        /// <summary>
        /// Opens the storage object with exclusive access to the most 
        /// recently committed version.
        /// </summary>
        Priority = 0x00040000,

        /// <summary>
        /// Indicates that the underlying file is to be automatically destroyed when the root 
        /// storage object is released. This feature is most useful for creating temporary files. 
        /// </summary>
        DeleteOnRelease = 0x04000000,

        /// <summary>
        /// Indicates that, in transacted mode, a temporary scratch file is usually used 
        /// to save modifications until the Commit method is called. 
        /// Specifying NoScratch permits the unused portion of the original file 
        /// to be used as work space instead of creating a new file for that purpose. 
        /// </summary>
        NoScratch = 0x00100000,

        /// <summary>
        /// Indicates that an existing storage object 
        /// or stream should be removed before the new object replaces it. 
        /// </summary>
        Create = 0x00001000,

        /// <summary>
        /// Creates the new object while preserving existing data in a stream named "Contents". 
        /// </summary>
        Convert = 0x00020000,

        /// <summary>
        /// Causes the create operation to fail if an existing object with the specified name exists.
        /// </summary>
        FailIfThere = 0x00000000,

        /// <summary>
        /// This flag is used when opening a storage object with Transacted 
        /// and without ShareExclusive or ShareDenyWrite. 
        /// In this case, specifying NoSnapshot prevents the system-provided 
        /// implementation from creating a snapshot copy of the file. 
        /// Instead, changes to the file are written to the end of the file. 
        /// </summary>
        NoSnapshot = 0x00200000,

        /// <summary>
        /// Supports direct mode for single-writer, multireader file operations. 
        /// </summary>
        DirectSingleWriterMultipleReader = 0x00400000
    };



    /// <summary>
    /// http://pinvoke.net/default.aspx/Enums/FOS.html
    /// </summary>
    [Flags]
    enum FOS : uint
    {
        FOS_OVERWRITEPROMPT = 0x00000002,
        FOS_STRICTFILETYPES = 0x00000004,
        FOS_NOCHANGEDIR = 0x00000008,
        FOS_PICKFOLDERS = 0x00000020,
        FOS_FORCEFILESYSTEM = 0x00000040, // Ensure that items returned are filesystem items.
        FOS_ALLNONSTORAGEITEMS = 0x00000080, // Allow choosing items that have no storage.
        FOS_NOVALIDATE = 0x00000100,
        FOS_ALLOWMULTISELECT = 0x00000200,
        FOS_PATHMUSTEXIST = 0x00000800,
        FOS_FILEMUSTEXIST = 0x00001000,
        FOS_CREATEPROMPT = 0x00002000,
        FOS_SHAREAWARE = 0x00004000,
        FOS_NOREADONLYRETURN = 0x00008000,
        FOS_NOTESTFILECREATE = 0x00010000,
        FOS_HIDEMRUPLACES = 0x00020000,
        FOS_HIDEPINNEDPLACES = 0x00040000,
        FOS_NODEREFERENCELINKS = 0x00100000,
        FOS_DONTADDTORECENT = 0x02000000,
        FOS_FORCESHOWHIDDEN = 0x10000000,
        FOS_DEFAULTNOMINIMODE = 0x20000000
    }



    internal enum LibraryFolderFilter
    {
        ForceFileSystem = 1,
        StorageItems = 2,
        AllItems = 3
    };

#if true
    /// <summary>
    /// http://pinvoke.net/default.aspx/Interfaces/IShellItem.html?diff=y
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid(ShellIIDGuid.IShellItem)]
    internal interface IShellItem
    {
        HResult BindToHandler(IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppv);

        HResult GetParent(out IShellItem ppsi);

        HResult GetDisplayName([In] ShellItemDesignNameOptions sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        //void GetDisplayName(ShellItemDesignNameOptions sigdnName, out IntPtr ppszName);

        //void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void GetAttributes(
            [In] ShellFileGetAttributesOptions sfgaoMask,
            out ShellFileGetAttributesOptions psfgaoAttribs);


        void Compare(IShellItem psi, uint hint, out int piOrder);
    }
#else
    [ComImport,
    Guid(ShellIIDGuid.IShellItem),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        // Not supported: IBindCtx.
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult BindToHandler(
            [In] IntPtr pbc,
            [In] ref Guid bhid,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellFolder ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        //[PreserveSig]
        //[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDisplayName(
            [In] ShellItemDesignNameOptions sigdnName,
            [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);//out IntPtr ppszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAttributes([In] ShellNativeMethods.ShellFileGetAttributesOptions sfgaoMask, out ShellNativeMethods.ShellFileGetAttributesOptions psfgaoAttribs);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult Compare(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            [In] SICHINTF hint,
            out int piOrder);
    }
#endif


    [ComImport,
    Guid(ShellIIDGuid.IShellItem2),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem2 : IShellItem
    {
        /*
        // Not supported: IBindCtx.
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult BindToHandler(
            [In] IntPtr pbc,
            [In] ref Guid bhid,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IShellFolder ppv);
        
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetDisplayName(
            [In] ShellItemDesignNameOptions sigdnName,
            [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAttributes([In] ShellFileGetAttributesOptions sfgaoMask, out ShellFileGetAttributesOptions psfgaoAttribs);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Compare(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            [In] uint hint,
            out int piOrder);
        */
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
        int GetPropertyStore(
            [In] GetPropertyStoreOptions Flags,
            [In] ref Guid riid,
            out IntPtr ppv);
        //int GetPropertyStore(
        //    [In] ShellNativeMethods.GetPropertyStoreOptions Flags,
        //    [In] ref Guid riid,
        //    [Out, MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetPropertyStoreWithCreateObject([In] GetPropertyStoreOptions Flags,
            [In, MarshalAs(UnmanagedType.IUnknown)] object punkCreateObject, [In] ref Guid riid, out IntPtr ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetPropertyStoreForKeys([In] ref PropertyKey rgKeys, [In] uint cKeys,
            [In] GetPropertyStoreOptions Flags, [In] ref Guid riid,
            out IntPtr ppv);
        //void GetPropertyStoreForKeys([In] ref PropertyKey rgKeys, [In] uint cKeys,
        //    [In] GetPropertyStoreOptions Flags, [In] ref Guid riid,
        //    [Out, MarshalAs(UnmanagedType.IUnknown)] out IPropertyStore ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetPropertyDescriptionList([In] ref PropertyKey keyType, [In] ref Guid riid, out IntPtr ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult Update([In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetProperty([In] ref PropertyKey key, IntPtr ppropvar);
        //void GetProperty([In] ref PropertyKey key, [Out] PropVariant ppropvar);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCLSID([In] ref PropertyKey key, out Guid pclsid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileTime([In] ref PropertyKey key, out System.Runtime.InteropServices.ComTypes.FILETIME pft);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetInt32([In] ref PropertyKey key, out int pi);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetString([In] ref PropertyKey key, [MarshalAs(UnmanagedType.LPWStr)] out string ppsz);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUInt32([In] ref PropertyKey key, out uint pui);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUInt64([In] ref PropertyKey key, out ulong pull);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetBool([In] ref PropertyKey key, out int pf);
    }


    /// <summary>
    /// Defines a unique key for a Shell Property
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PropertyKey// : IEquatable<PropertyKey>
    {
        public Guid formatId;
        public Int32 propertyId;
    }

    [ComImport,
    Guid(ShellIIDGuid.IShellItemArray),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItemArray
    {
        // Not supported: IBindCtx.
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult BindToHandler(
            [In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc,
            [In] ref Guid rbhid,
            [In] ref Guid riid,
            out IntPtr ppvOut);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetPropertyStore(
            [In] int Flags,
            [In] ref Guid riid,
            out IntPtr ppv);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetPropertyDescriptionList(
            [In] ref PropertyKey keyType,
            [In] ref Guid riid,
            out IntPtr ppv);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetAttributes(
            [In] ShellItemAttributeOptions dwAttribFlags,
            [In] ShellFileGetAttributesOptions sfgaoMask,
            out ShellFileGetAttributesOptions psfgaoAttribs);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetCount(out uint pdwNumItems);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetItemAt(
            [In] uint dwIndex,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        // Not supported: IEnumShellItems (will use GetCount and GetItemAt instead).
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
    }



    /// <summary>
    /// http://pinvoke.net/default.aspx/Interfaces.IFileOpenDialog
    /// </summary>
    [ComImport, Guid(ShellIIDGuid.IFileOpenDialog), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileOpenDialog //: IFileDialog
    {
        // Defined on IModalWindow - repeated here due to requirements of COM interop layer
        // --------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
        HResult Show([In] IntPtr parent);

        // Defined on IFileDialog - repeated here due to requirements of COM interop layer
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypes();// ([In] uint cFileTypes, [In] COMDLG_FILTERSPEC[] rgFilterSpec);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypeIndex([In] uint iFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileTypeIndex(out uint piFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Advise();// ([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Unadvise([In] uint dwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptions([In] FOS fos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOptions(out FOS pfos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, int fdap);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Close([MarshalAs(UnmanagedType.Error)] int hr);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetClientGuid([In] ref Guid guid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ClearClientData();

        // Not supported:  IShellItemFilter is not defined, converting to IntPtr
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

        // Defined by IFileOpenDialog
        // ---------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppsai);
    }



    [ComImport,
    Guid(ShellIIDGuid.IShellLibrary),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellLibrary
    {
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult LoadLibraryFromItem(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem library,
            [In] AccessModes grfMode);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void LoadLibraryFromKnownFolder(
            [In] ref Guid knownfidLibrary,
            [In] AccessModes grfMode);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem location);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem location);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult GetFolders(
            [In] LibraryFolderFilter lff,
            [In] ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ResolveFolder(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem folderToResolve,
            [In] uint timeout,
            [In] ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDefaultSaveFolder(
            [In] int dsft,
            [In] ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultSaveFolder(
            [In] int dsft,
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem si);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOptions(
            out int lofOptions);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptions(
            [In] int lofMask,
            [In] int lofOptions);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFolderType(out Guid ftid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFolderType([In] ref Guid ftid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetIcon([MarshalAs(UnmanagedType.LPWStr)] out string icon);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetIcon([In, MarshalAs(UnmanagedType.LPWStr)] string icon);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Commit();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Save(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem folderToSaveIn,
            [In, MarshalAs(UnmanagedType.LPWStr)] string libraryName,
            [In] int lsf,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem2 savedTo);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SaveInKnownFolder(
            [In] ref Guid kfidToSaveIn,
            [In, MarshalAs(UnmanagedType.LPWStr)] string libraryName,
            [In] int lsf,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem2 savedTo);
    };

    [ComImport,
    Guid(ShellIIDGuid.IShellFolder),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    ComConversionLoss]
    internal interface IShellFolder
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ParseDisplayName(IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In, MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, [In, Out] ref uint pchEaten, [Out] IntPtr ppidl, [In, Out] ref uint pdwAttributes);
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult EnumObjects([In] IntPtr hwnd, [In] int grfFlags, [MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenumIDList);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult BindToObject([In] IntPtr pidl, /*[In, MarshalAs(UnmanagedType.Interface)] IBindCtx*/ IntPtr pbc, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IShellFolder ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BindToStorage([In] ref IntPtr pidl, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In] ref Guid riid, out IntPtr ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CompareIDs([In] IntPtr lParam, [In] ref IntPtr pidl1, [In] ref IntPtr pidl2);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void CreateViewObject([In] IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetAttributesOf([In] uint cidl, [In] IntPtr apidl, [In, Out] ref uint rgfInOut);


        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetUIObjectOf([In] IntPtr hwndOwner, [In] uint cidl, [In] IntPtr apidl, [In] ref Guid riid, [In, Out] ref uint rgfReserved, out IntPtr ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDisplayNameOf([In] ref IntPtr pidl, [In] uint uFlags, out IntPtr pName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetNameOf([In] IntPtr hwnd, [In] ref IntPtr pidl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszName, [In] uint uFlags, [Out] IntPtr ppidlOut);
    }


    [ComImport,
    Guid(ShellIIDGuid.IEnumIDList),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnumIDList
    {
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult Next(uint celt, out IntPtr rgelt, out uint pceltFetched);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult Skip([In] uint celt);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult Reset();

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        HResult Clone([MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenum);
    }


    /// <summary>
    /// The base class for all Shell objects in Shell Namespace.
    /// </summary>
    abstract public class ShellObject : IDisposable//, IEquatable<ShellObject>
    {


        #region Internal Fields

        /// <summary>
        /// Internal member to keep track of the native IShellItem2
        /// </summary>
        internal IShellItem2 nativeShellItem;

        #endregion

        #region Constructors

        internal ShellObject()
        {
        }

        internal ShellObject(IShellItem2 shellItem)
        {
            nativeShellItem = shellItem;
        }

        #endregion

        #region Protected Fields

        /// <summary>
        /// Parsing name for this Object e.g. c:\Windows\file.txt,
        /// or ::{Some Guid} 
        /// </summary>
        private string _internalParsingName;

        /// <summary>
        /// A friendly name for this object that' suitable for display
        /// </summary>
        private string _internalName;

        /// <summary>
        /// PID List (PIDL) for this object
        /// </summary>
        private IntPtr _internalPIDL = IntPtr.Zero;

        #endregion

        #region Internal Properties

        /// <summary>
        /// Return the native ShellFolder object as newer IShellItem2
        /// </summary>
        /// <exception cref="System.Runtime.InteropServices.ExternalException">If the native object cannot be created.
        /// The ErrorCode member will contain the external error code.</exception>
        virtual internal IShellItem2 NativeShellItem2
        {
            get
            {
                if (nativeShellItem == null && ParsingName != null)
                {
                    Guid guid = new Guid(ShellIIDGuid.IShellItem2);
                    int retCode = ShellNativeMethods.SHCreateItemFromParsingName(
                        ParsingName, IntPtr.Zero, ref guid, out nativeShellItem);

                    if (nativeShellItem == null || !CoreErrorHelper.Succeeded(retCode))
                    {
                        throw new Exception("LocalizedMessages.ShellObjectCreationFailed",
                            Marshal.GetExceptionForHR(retCode));
                    }
                }
                return nativeShellItem;
            }
        }

        /// <summary>
        /// Return the native ShellFolder object
        /// </summary>
        virtual internal IShellItem NativeShellItem
        {
            get { return NativeShellItem2; }
        }

        #endregion


        #region Public Properties


        /// <summary>
        /// Gets the parsing name for this ShellItem.
        /// </summary>
        virtual public string ParsingName
        {
            get
            {
                if (_internalParsingName == null && nativeShellItem != null)
                {
                    _internalParsingName = ShellItemHelper.GetParsingName(nativeShellItem);
                }
                return _internalParsingName ?? string.Empty;
            }
            protected set
            {
                _internalParsingName = value;
            }
        }

        /// <summary>
        /// Gets the normal display for this ShellItem.
        /// </summary>
        virtual public string Name
        {
            get
            {
                if (_internalName == null && NativeShellItem != null)
                {
                    _internalName = ShellItemHelper.GetNormalName(NativeShellItem);
                }
                return _internalName;
            }

            protected set
            {
                this._internalName = value;
            }
        }


        /// <summary>
        /// Overrides object.ToString()
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Release the native and managed objects
        /// </summary>
        /// <param name="disposing">Indicates that this is being called from Dispose(), rather than the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _internalName = null;
                _internalParsingName = null;
                //properties = null;
                //thumbnail = null;
                //parentShellObject = null;
            }
            /*
            if (properties != null)
            {
                properties.Dispose();
            }*/

            if (_internalPIDL != IntPtr.Zero)
            {
                ShellNativeMethods.ILFree(_internalPIDL);
                _internalPIDL = IntPtr.Zero;
            }

            if (nativeShellItem != null)
            {
                Marshal.ReleaseComObject(nativeShellItem);
                nativeShellItem = null;
            }
            /*
            if (NativePropertyStore != null)
            {
                Marshal.ReleaseComObject(NativePropertyStore);
                NativePropertyStore = null;
            }*/
        }

        /// <summary>
        /// Release the native objects.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implement the finalizer.
        /// </summary>
        ~ShellObject()
        {
            Dispose(false);
        }

        #endregion

    }


    /// <summary>
    /// Represents the base class for all types of Shell "containers". Any class deriving from this class
    /// can contain other ShellObjects (e.g. ShellFolder, FileSystemKnownFolder, ShellLibrary, etc)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
        "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "This will complicate the class hierarchy and naming convention used in the Shell area")]
    public abstract class ShellContainer : ShellObject, /*IEnumerable<ShellObject>, */IDisposable
    {

        #region Private Fields

        private IShellFolder desktopFolderEnumeration;
        private IShellFolder nativeShellFolder;

        #endregion


        #region Internal Constructor

        internal ShellContainer() { }

        internal ShellContainer(IShellItem2 shellItem) : base(shellItem) { }

        #endregion

        #region Disposable Pattern

        /// <summary>
        /// Release resources
        /// </summary>
        /// <param name="disposing"><B>True</B> indicates that this is being called from Dispose(), rather than the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (nativeShellFolder != null)
            {
                Marshal.ReleaseComObject(nativeShellFolder);
                nativeShellFolder = null;
            }

            if (desktopFolderEnumeration != null)
            {
                Marshal.ReleaseComObject(desktopFolderEnumeration);
                desktopFolderEnumeration = null;
            }

            base.Dispose(disposing);
        }

        #endregion

    }

    /// <summary>
    /// Represents the base class for all types of folders (filesystem and non filesystem)
    /// </summary>
    public abstract class ShellFolder : ShellContainer
    {
    }
    /// <summary>
    /// A folder in the Shell Namespace
    /// </summary>
    public class ShellFileSystemFolder : ShellFolder
    {
        internal ShellFileSystemFolder()
        {
        }

        internal ShellFileSystemFolder(IShellItem2 shellItem)
        {
            nativeShellItem = shellItem;
        }

        /// <summary>
        /// The path for this Folder
        /// </summary>
        public virtual string Path
        {
            get { return this.ParsingName; }
        }
    }

    /// <summary>
    /// A Shell Library in the Shell Namespace
    /// </summary>
    public sealed class ShellLibrary : ShellContainer, IEnumerable<ShellFileSystemFolder>
    {
        #region Private Fields

        private INativeShellLibrary nativeShellLibrary;
        //private IKnownFolder knownFolder;

        private static Guid[] FolderTypesGuids =
        {
            new Guid(ShellKFIDGuid.GenericLibrary),
            new Guid(ShellKFIDGuid.DocumentsLibrary),
            new Guid(ShellKFIDGuid.MusicLibrary),
            new Guid(ShellKFIDGuid.PicturesLibrary),
            new Guid(ShellKFIDGuid.VideosLibrary)
        };

        #endregion

        #region Private Constructor

        private ShellLibrary()
        {
            //CoreHelpers.ThrowIfNotWin7();
        }

        //Construct the ShellLibrary object from a native Shell Library
        private ShellLibrary(INativeShellLibrary nativeShellLibrary)
            : this()
        {
            this.nativeShellLibrary = nativeShellLibrary;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// The name of the library, every library must 
        /// have a name
        /// </summary>
        /// <exception cref="COMException">Will throw if no Icon is set</exception>
        public override string Name
        {
            get
            {
                if (base.Name == null && NativeShellItem != null)
                {
                    base.Name = System.IO.Path.GetFileNameWithoutExtension
                        (ShellItemHelper.GetParsingName(NativeShellItem));
                }

                return base.Name;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Close the library, and release its associated file system resources
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        #endregion

        #region Internal Properties

        internal const string FileExtension = ".library-ms";

        internal override IShellItem NativeShellItem
        {
            get { return NativeShellItem2; }
        }

        internal override IShellItem2 NativeShellItem2
        {
            get { return nativeShellItem; }
        }

        #endregion

        #region Static Shell Library methods

        /// <summary>
        /// Load the library using a number of options
        /// </summary>
        /// <param name="libraryName">The name of the library.</param>
        /// <param name="folderPath">The path to the library.</param>
        /// <param name="isReadOnly">If <B>true</B>, opens the library in read-only mode.</param>
        /// <returns>A ShellLibrary Object</returns>
        public static ShellLibrary Load(string libraryName, string folderPath, bool isReadOnly)
        {
            //CoreHelpers.ThrowIfNotWin7();

            // Create the shell item path
            string shellItemPath = System.IO.Path.Combine(folderPath, libraryName + FileExtension);
            //ShellFile item = ShellFile.FromFilePath(shellItemPath);
            //var item = new ShellFile(shellItemPath);

            IShellItem nativeShellItem;//= item.NativeShellItem;

            {
                var path = shellItemPath;
                // Get the absolute path
                string absPath = (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                    ? path : Path.GetFullPath(path);

                // Make sure this is valid
                if (!File.Exists(absPath))
                {
                    throw new FileNotFoundException(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "LocalizedMessages.FilePathNotExist", path));
                }

                var ParsingName = absPath;

                IShellItem2 nativeShellItem_ = null;
                if (nativeShellItem_ == null && ParsingName != null)
                {
                    Guid guid = new Guid(ShellIIDGuid.IShellItem2);
                    int retCode = ShellNativeMethods.SHCreateItemFromParsingName(
                        ParsingName, IntPtr.Zero, ref guid, out nativeShellItem_);

                    if (nativeShellItem_ == null || !CoreErrorHelper.Succeeded(retCode))
                    {
                        throw new Exception("LocalizedMessages.ShellObjectCreationFailed",
                            Marshal.GetExceptionForHR(retCode));
                    }
                }
                nativeShellItem = nativeShellItem_;

            }
            INativeShellLibrary nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();
            AccessModes flags = isReadOnly ?
                    AccessModes.Read :
                    AccessModes.ReadWrite;
            nativeShellLibrary.LoadLibraryFromItem(nativeShellItem, flags);

            ShellLibrary library = new ShellLibrary(nativeShellLibrary);
            try
            {
                library.nativeShellItem = (IShellItem2)nativeShellItem;
                library.Name = libraryName;

                return library;
            }
            catch
            {
                library.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load the library using a number of options
        /// </summary>
        /// <param name="nativeShellItem">IShellItem</param>
        /// <param name="isReadOnly">read-only flag</param>
        /// <returns>A ShellLibrary Object</returns>
        internal static ShellLibrary FromShellItem(IShellItem nativeShellItem, bool isReadOnly)
        {
            //CoreHelpers.ThrowIfNotWin7();

            INativeShellLibrary nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();

            AccessModes flags = isReadOnly ?
                    AccessModes.Read :
                    AccessModes.ReadWrite;

            nativeShellLibrary.LoadLibraryFromItem(nativeShellItem, flags);

            ShellLibrary library = new ShellLibrary(nativeShellLibrary);
            library.nativeShellItem = (IShellItem2)nativeShellItem;

            return library;
        }
        #endregion


        #region Disposable Pattern

        /// <summary>
        /// Release resources
        /// </summary>
        /// <param name="disposing">Indicates that this was called from Dispose(), rather than from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (nativeShellLibrary != null)
            {
                Marshal.ReleaseComObject(nativeShellLibrary);
                nativeShellLibrary = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Release resources
        /// </summary>
        ~ShellLibrary()
        {
            Dispose(false);
        }

        #endregion

        #region Private Properties

        private List<ShellFileSystemFolder> ItemsList
        {
            get { return GetFolders(); }
        }

        private List<ShellFileSystemFolder> GetFolders()
        {
            List<ShellFileSystemFolder> list = new List<ShellFileSystemFolder>();
            IShellItemArray itemArray;

            Guid shellItemArrayGuid = new Guid(ShellIIDGuid.IShellItemArray);

            HResult hr = nativeShellLibrary.GetFolders(
                LibraryFolderFilter.AllItems, ref shellItemArrayGuid, out itemArray);

            if (!CoreErrorHelper.Succeeded(hr)) { return list; }

            uint count;
            itemArray.GetCount(out count);

            for (uint i = 0; i < count; ++i)
            {
                IShellItem shellItem;
                itemArray.GetItemAt(i, out shellItem);
                list.Add(new ShellFileSystemFolder(shellItem as IShellItem2));
            }

            if (itemArray != null)
            {
                Marshal.ReleaseComObject(itemArray);
                itemArray = null;
            }

            return list;
        }

        #endregion

        #region IEnumerable<ShellFileSystemFolder> Members

        /// <summary>
        /// Retrieves the collection enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<ShellFileSystemFolder> GetEnumerator()
        {
            return ItemsList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Retrieves the collection enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ItemsList.GetEnumerator();
        }

        #endregion
    }


    // .NET classes representing runtime callable wrappers
    [ComImport,
    ClassInterface(ClassInterfaceType.None),
    TypeLibType(TypeLibTypeFlags.FCanCreate),
    Guid(ShellCLSIDGuid.FileOpenDialog)]
    internal class FileOpenDialogRCW
    {
    }

    [ComImport,
    Guid(ShellIIDGuid.IShellLibrary),
    CoClass(typeof(ShellLibraryCoClass))]
    internal interface INativeShellLibrary : IShellLibrary
    {
    }

    [ComImport,
    ClassInterface(ClassInterfaceType.None),
    TypeLibType(TypeLibTypeFlags.FCanCreate),
    Guid(ShellCLSIDGuid.ShellLibrary)]
    internal class ShellLibraryCoClass
    {
    }


    internal static class ShellNativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            // The following parameter is not used - binding context.
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem2 shellItem);


        [DllImport("shell32.dll", CharSet = CharSet.None)]
        public static extern void ILFree(IntPtr pidl);


        [DllImport("shell32.dll")]
        internal static extern int SHILCreateFromPath(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath, out IntPtr ppIdl, ref uint rgflnOut);

        [DllImport("shell32.dll")]
        internal static extern int SHCreateShellItem(
            IntPtr pidlParent, IntPtr psfParent, IntPtr pidl, out IShellItem ppsi);

    }


    /// <summary>
    /// Provide Error Message Helper Methods.
    /// This is intended for Library Internal use only.
    /// </summary>
    internal static class CoreErrorHelper
    {
        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        private const int FacilityWin32 = 7;

        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        public const int Ignored = (int)HResult.Ok;

        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        /// <param name="win32ErrorCode">The Windows API error code.</param>
        /// <returns>The equivalent HRESULT.</returns>
        public static int HResultFromWin32(int win32ErrorCode)
        {
            if (win32ErrorCode > 0)
            {
                win32ErrorCode =
                    (int)(((uint)win32ErrorCode & 0x0000FFFF) | (FacilityWin32 << 16) | 0x80000000);
            }
            return win32ErrorCode;

        }

        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates success.</returns>
        public static bool Succeeded(int result)
        {
            return result >= 0;
        }

        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates success.</returns>
        public static bool Succeeded(HResult result)
        {
            return Succeeded((int)result);
        }

        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates failure.</returns>
        public static bool Failed(HResult result)
        {
            return !Succeeded(result);
        }

        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates failure.</returns>
        public static bool Failed(int result)
        {
            return !Succeeded(result);
        }

        /// <summary>
        /// This is intended for Library Internal use only.
        /// </summary>
        /// <param name="result">The COM error code.</param>
        /// <param name="win32ErrorCode">The Win32 error code.</param>
        /// <returns>Inticates that the Win32 error code corresponds to the COM error code.</returns>
        public static bool Matches(int result, int win32ErrorCode)
        {
            return (result == HResultFromWin32(win32ErrorCode));
        }
    }


    /// <summary>
    /// A helper class for Shell Objects
    /// </summary>
    internal static class ShellItemHelper
    {
        internal static string GetParsingName(IShellItem shellItem)
        {
            if (shellItem == null) { return null; }

            shellItem.GetDisplayName(
                ShellItemDesignNameOptions.DesktopAbsoluteParsing,
                out var path);
            return path;
        }
        internal static string GetNormalName(IShellItem shellItem)
        {
            if (shellItem == null) { return null; }

            shellItem.GetDisplayName(
                ShellItemDesignNameOptions.Normal,
                out var path);
            return path;
        }
    }



    internal static class ShellIIDGuid
    {

        // IID GUID strings for relevant Shell COM interfaces.
        internal const string IModalWindow = "B4DB1657-70D7-485E-8E3E-6FCB5A5C1802";
        internal const string IFileDialog = "42F85136-DB7E-439C-85F1-E4075D135FC8";
        internal const string IFileOpenDialog = "D57C7288-D4AD-4768-BE02-9D969532D960";
        internal const string IFileSaveDialog = "84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB";
        internal const string IFileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
        internal const string IFileDialogControlEvents = "36116642-D713-4B97-9B83-7484A9D00433";
        internal const string IFileDialogCustomize = "E6FDD21A-163F-4975-9C8C-A69F1BA37034";

        internal const string IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
        internal const string IShellItem2 = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";
        internal const string IShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";
        internal const string IShellLibrary = "11A66EFA-382E-451A-9234-1E0E12EF3085";
        internal const string IThumbnailCache = "F676C15D-596A-4ce2-8234-33996F445DB1";
        internal const string ISharedBitmap = "091162a4-bc96-411f-aae8-c5122cd03363";
        internal const string IShellFolder = "000214E6-0000-0000-C000-000000000046";
        internal const string IShellFolder2 = "93F2F68C-1D1B-11D3-A30E-00C04F79ABD1";
        internal const string IEnumIDList = "000214F2-0000-0000-C000-000000000046";
        internal const string IShellLinkW = "000214F9-0000-0000-C000-000000000046";
        internal const string CShellLink = "00021401-0000-0000-C000-000000000046";

        internal const string IPropertyStore = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99";
        internal const string IPropertyStoreCache = "3017056d-9a91-4e90-937d-746c72abbf4f";
        internal const string IPropertyDescription = "6F79D558-3E96-4549-A1D1-7D75D2288814";
        internal const string IPropertyDescription2 = "57D2EDED-5062-400E-B107-5DAE79FE57A6";
        internal const string IPropertyDescriptionList = "1F9FC1D0-C39B-4B26-817F-011967D3440E";
        internal const string IPropertyEnumType = "11E1FBF9-2D56-4A6B-8DB3-7CD193A471F2";
        internal const string IPropertyEnumType2 = "9B6E051C-5DDD-4321-9070-FE2ACB55E794";
        internal const string IPropertyEnumTypeList = "A99400F4-3D84-4557-94BA-1242FB2CC9A6";
        internal const string IPropertyStoreCapabilities = "c8e2d566-186e-4d49-bf41-6909ead56acc";

        internal const string ICondition = "0FC988D4-C935-4b97-A973-46282EA175C8";
        internal const string ISearchFolderItemFactory = "a0ffbc28-5482-4366-be27-3e81e78e06c2";
        internal const string IConditionFactory = "A5EFE073-B16F-474f-9F3E-9F8B497A3E08";
        internal const string IRichChunk = "4FDEF69C-DBC9-454e-9910-B34F3C64B510";
        internal const string IPersistStream = "00000109-0000-0000-C000-000000000046";
        internal const string IPersist = "0000010c-0000-0000-C000-000000000046";
        internal const string IEnumUnknown = "00000100-0000-0000-C000-000000000046";
        internal const string IQuerySolution = "D6EBC66B-8921-4193-AFDD-A1789FB7FF57";
        internal const string IQueryParser = "2EBDEE67-3505-43f8-9946-EA44ABC8E5B0";
        internal const string IQueryParserManager = "A879E3C4-AF77-44fb-8F37-EBD1487CF920";
    }

    internal static class ShellCLSIDGuid
    {

        // CLSID GUID strings for relevant coclasses.
        internal const string FileOpenDialog = "DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7";
        internal const string FileSaveDialog = "C0B4E2F3-BA21-4773-8DBA-335EC946EB8B";
        internal const string KnownFolderManager = "4DF0C730-DF9D-4AE3-9153-AA6B82E9795A";
        internal const string ShellLibrary = "D9B3211D-E57F-4426-AAEF-30A806ADD397";
        internal const string SearchFolderItemFactory = "14010e02-bbbd-41f0-88e3-eda371216584";
        internal const string ConditionFactory = "E03E85B0-7BE3-4000-BA98-6C13DE9FA486";
        internal const string QueryParserManager = "5088B39A-29B4-4d9d-8245-4EE289222F66";
    }

    internal static class ShellKFIDGuid
    {

        internal const string ComputerFolder = "0AC0837C-BBF8-452A-850D-79D08E667CA7";
        internal const string Favorites = "1777F761-68AD-4D8A-87BD-30B759FA33DD";
        internal const string Documents = "FDD39AD0-238F-46AF-ADB4-6C85480369C7";
        internal const string Profile = "5E6C858F-0E22-4760-9AFE-EA3317B67173";

        internal const string GenericLibrary = "5c4f28b5-f869-4e84-8e60-f11db97c5cc7";
        internal const string DocumentsLibrary = "7d49d726-3c21-4f05-99aa-fdc2c9474656";
        internal const string MusicLibrary = "94d6ddcc-4a68-4175-a374-bd584a510b78";
        internal const string PicturesLibrary = "b3690e58-e961-423b-b687-386ebfd83239";
        internal const string VideosLibrary = "5fa96407-7e77-483c-ac93-691d05850de8";

        internal const string Libraries = "1B3EA5DC-B587-4786-B4EF-BD1DC332AEAE";
    }
}
