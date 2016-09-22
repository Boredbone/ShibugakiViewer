using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInstaller;

namespace EditMsi
{
    class Program
    {

        static void Update
            (WindowsInstaller.Database database, string table, string filter, int column, string value)
        {
            var view = database.OpenView($@"Select * FROM {table} WHERE {filter}");
            view.Execute();
            var record = view.Fetch();
            record.StringData[column] = value;
            view.Modify(MsiViewModify.msiViewModifyUpdate, record);
        }


        static void Main(string[] args)
        {
            //"C:\Windows\System32\msi.dll"

            var path = @"Release\SetupProject.msi";

            var msi = new WindowsInstallerClass() as WindowsInstaller.Installer;


            WindowsInstaller.Database database =
                msi.OpenDatabase(path, WindowsInstaller.MsiOpenDatabaseMode.msiOpenDatabaseModeTransact);

            var ExitAppName = "_2548E01E_23FC_4621_9D97_67C658AC7578";
            var LaunchAppName = "_E7893FAB_E790_4DDD_B0B5_DC7AACC28A69";

            Update(database, "InstallExecuteSequence", $@"Action = '{ExitAppName}'", 3, "1399");
            Update(database, "CustomAction", $@"Action = '{ExitAppName}'", 2, "1");

            
            Update(database, "CustomAction", $@"Action = '{LaunchAppName}'", 2, "6");

            Update(database, "ControlEvent", $@"Dialog_ = 'FinishedForm' AND Argument = 'Return'", 6, "1");


            {
                var view = database
                    .OpenView($@"DELETE FROM InstallExecuteSequence WHERE Action = '{LaunchAppName}'");
                view.Execute();
            }


            {
                var sql = $@"INSERT INTO ControlEvent (Dialog_,Control_,Event,Argument,Condition,Ordering)"
                    + $@" VALUES('FinishedForm','CloseButton','DoAction','{LaunchAppName}','1','0')";
                database.OpenView(sql).Execute();
            }

            {
                var sql = $@"INSERT INTO Property (Property,Value) VALUES('DISABLEADVTSHORTCUTS','1')";
                database.OpenView(sql).Execute();
            }

            database.Commit();
        }
    }

    [System.Runtime.InteropServices.ComImport(),
        System.Runtime.InteropServices.Guid("000C1090-0000-0000-C000-000000000046")]
    class WindowsInstallerClass { }
}
