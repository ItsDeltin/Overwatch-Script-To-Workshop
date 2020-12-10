using System;
using System.Collections.Generic;
using System.IO;
using WorkspaceFolder = OmniSharp.Extensions.LanguageServer.Protocol.Models.WorkspaceFolder;
using WorkspaceFolderContainer = OmniSharp.Extensions.LanguageServer.Protocol.Models.Container<OmniSharp.Extensions.LanguageServer.Protocol.Models.WorkspaceFolder>;

namespace Deltin.Deltinteger.LanguageServer
{
    public class ServerWorkspace
    {
        public List<WorkspaceFolder> WorkspaceFolders { get; } = new List<WorkspaceFolder>();

        public void SetWorkspaceFolders(WorkspaceFolderContainer container)
        {
            if (container != null)
                foreach (var folder in container)
                    WorkspaceFolders.Add(folder);
        }

        public WorkspaceFolder GetFileWorkspaceFolder(Uri uri)
        {
            foreach (var folder in WorkspaceFolders)
                if (folder.Uri.ToUri().IsBaseOf(uri))
                    return folder;
            return null;
        }
    }
}