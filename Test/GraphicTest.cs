using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.Creation;
using ImageLibrary.File;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Extensions;
using Boredbone.Utility.Tools;

namespace Test
{
    [TestClass]
    public class GraphicTest
    {
        [TestMethod]
        public void GraphicInformationTest()
        {

            var path = @"..\..\..\ShibugakiViewer\Assets\Icons\mikan_rect64.png";
            var information = new GraphicInformation(path);

            Assert.AreEqual(GraphicFileType.Png, information.Type);
            Assert.AreEqual(new System.Drawing.Size(64, 64), information.GraphicSize);
        }
    }
}
