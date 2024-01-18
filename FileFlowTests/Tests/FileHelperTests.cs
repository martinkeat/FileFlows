using System.IO;
using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlowTests.Tests;

[TestClass]
public class FileHelperTests
{
    
    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_ValidPermissions_ReturnsCorrectEnum()
    {
        // Arrange
        int linuxPermissions = 755; // Example valid Linux permission value

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute, result);
    }

    
    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_777()
    {
        // Arrange
        int linuxPermissions = 777; // Example valid Linux permission value

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | 
                        UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute, result);
    }
    
    
    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_666()
    {
        // Arrange
        int linuxPermissions = 666; // Example valid Linux permission value

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite |
                        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | 
                        UnixFileMode.OtherRead | UnixFileMode.OtherWrite, result);
    }
    
    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_660()
    {
        // Arrange
        int linuxPermissions = 660; // Example valid Linux permission value

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite |
                        UnixFileMode.GroupRead | UnixFileMode.GroupWrite , result);
    }
    
    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_PermissionsOutOfRange_ReturnsNone()
    {
        // Arrange
        int linuxPermissions = 800; // Example out of range Linux permission value

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.None, result);
    }

    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_NegativePermissions_ReturnsNone()
    {
        // Arrange
        int linuxPermissions = -1; // Example negative Linux permission value

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.None, result);
    }
    
    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_UserReadWriteExecute_ReturnsCorrectEnum()
    {
        // Arrange
        int linuxPermissions = 700; // User has read, write, and execute permissions, others have none

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, result);
    }

    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_GroupReadExecute_ReturnsCorrectEnum()
    {
        // Arrange
        int linuxPermissions = 050; // Group has read and execute permissions, others have none

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.GroupRead | UnixFileMode.GroupExecute, result);
    }

    [TestMethod]
    public void ConvertLinuxPermissionsToUnixFileMode_OtherWrite_ReturnsCorrectEnum()
    {
        // Arrange
        int linuxPermissions = 002; // Others have write permission, user and group have none

        // Act
        UnixFileMode result = FileHelper.ConvertLinuxPermissionsToUnixFileMode(linuxPermissions);

        // Assert
        Assert.AreEqual(UnixFileMode.OtherWrite, result);
    }
}