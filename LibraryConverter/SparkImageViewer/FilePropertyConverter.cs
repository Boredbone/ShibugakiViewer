using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryConverter.Compat
{
    public static class FilePropertyConverter
    {
        public static ImageLibrary.SearchProperty.FileProperty Convert
            (this SparkImageViewer.DataModel.FileProperty property)
        {
            switch (property)
            {
                case SparkImageViewer.DataModel.FileProperty.RelativePath:
                    return ImageLibrary.SearchProperty.FileProperty.FullPath;
                case SparkImageViewer.DataModel.FileProperty.RootDirectoryAccessToken:
                    return ImageLibrary.SearchProperty.FileProperty.DirectoryPathStartsWith;
                case SparkImageViewer.DataModel.FileProperty.Path:
                    return ImageLibrary.SearchProperty.FileProperty.DirectoryPathStartsWith;
                case SparkImageViewer.DataModel.FileProperty.DateTimeCreated:
                    return ImageLibrary.SearchProperty.FileProperty.DateTimeCreated;
                case SparkImageViewer.DataModel.FileProperty.DateTimeModified:
                    return ImageLibrary.SearchProperty.FileProperty.DateTimeModified;
                case SparkImageViewer.DataModel.FileProperty.DateTimeRegistered:
                    return ImageLibrary.SearchProperty.FileProperty.DateTimeRegistered;
                case SparkImageViewer.DataModel.FileProperty.DateCreated:
                    return ImageLibrary.SearchProperty.FileProperty.DateCreated;
                case SparkImageViewer.DataModel.FileProperty.DateModified:
                    return ImageLibrary.SearchProperty.FileProperty.DateModified;
                case SparkImageViewer.DataModel.FileProperty.DateRegistered:
                    return ImageLibrary.SearchProperty.FileProperty.DateRegistered;
                case SparkImageViewer.DataModel.FileProperty.FolderRelativeId:
                    return ImageLibrary.SearchProperty.FileProperty.FileName;
                case SparkImageViewer.DataModel.FileProperty.Width:
                    return ImageLibrary.SearchProperty.FileProperty.Width;
                case SparkImageViewer.DataModel.FileProperty.Height:
                    return ImageLibrary.SearchProperty.FileProperty.Height;
                case SparkImageViewer.DataModel.FileProperty.Size:
                    return ImageLibrary.SearchProperty.FileProperty.Size;
                case SparkImageViewer.DataModel.FileProperty.Tags:
                    return ImageLibrary.SearchProperty.FileProperty.ContainsTag;
                case SparkImageViewer.DataModel.FileProperty.NumOfTags:
                    return ImageLibrary.SearchProperty.FileProperty.HasTag;
                case SparkImageViewer.DataModel.FileProperty.Rating:
                    return ImageLibrary.SearchProperty.FileProperty.Rating;
                case SparkImageViewer.DataModel.FileProperty.GroupLeader:
                    return ImageLibrary.SearchProperty.FileProperty.Group;
                case SparkImageViewer.DataModel.FileProperty.FileName:
                    return ImageLibrary.SearchProperty.FileProperty.FileName;
                case SparkImageViewer.DataModel.FileProperty.FileNameContains:
                    return ImageLibrary.SearchProperty.FileProperty.FileNameContains;
                case SparkImageViewer.DataModel.FileProperty.RelativePathSequenceNum:
                    return ImageLibrary.SearchProperty.FileProperty.FileNameSequenceNumRight;
                case SparkImageViewer.DataModel.FileProperty.AspectRatio:
                    return ImageLibrary.SearchProperty.FileProperty.AspectRatio;
                default:
                    return ImageLibrary.SearchProperty.FileProperty.Id;
            }



        }
    }
}
