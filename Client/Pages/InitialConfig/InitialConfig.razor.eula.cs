namespace FileFlows.Client.Pages;

/// <summary>
/// The eula
/// </summary>
public partial class InitialConfig
{
    private readonly string EULA = @"
This End-User License Agreement (""EULA"") is a legal agreement between you and FileFlows.

This EULA agreement governs your acquisition and use of our FileFlows software (""Software"") directly from FileFlows.

Please read this EULA agreement carefully before completing the installation process and using the FileFlows software. It provides a license to use the FileFlows software and contains warranty information and liability disclaimers.

By accepting, you are confirming your acceptance of the Software and agreeing to become bound by the terms of this EULA agreement.

If you are entering into this EULA agreement on behalf of a company or other legal entity, you represent that you have the authority to bind such entity and its affiliates to these terms and conditions. If you do not have such authority or if you do not agree with the terms and conditions of this EULA agreement, do not install or use the Software, and you must not accept this EULA agreement.

This EULA agreement shall apply only to the Software supplied by FileFlows herewith regardless of whether other software is referred to or described herein. The terms also apply to any FileFlows updates, supplements, Internet-based services, and support services for the Software, unless other terms accompany those items on delivery. If so, those terms apply.

# **License Grant**

FileFlows hereby grants you a personal, non-transferable, non-exclusive licence to use the FileFlows software on your devices in accordance with the terms of this EULA agreement.

You are permitted to load the FileFlows software (for example a PC, laptop, mobile or tablet) under your control. You are responsible for ensuring your device meets the minimum requirements of the FileFlows software.

**You are not permitted to:**

* Edit, alter, modify, adapt, translate or otherwise change the whole or any part of the Software nor permit the whole or any part of the Software to be combined with or become incorporated in any other software, nor decompile, disassemble or reverse engineer the Software or attempt to do any such things
* Reproduce, copy, distribute, resell or otherwise use the Software for any commercial purpose
* Use the Software in a commercial environment. This is for personal use only.
* Allow any third party to use the Software on behalf of or for the benefit of any third party
* Use the Software in any way which breaches any applicable local, national or international law
* Use the Software for any purpose that FileFlows considers is a breach of this EULA agreement

**Paid License for Commercial Use:**

Users who obtain a paid license gain expanded rights, including the ability to use the software for commercial purposes. By purchasing a license, users agree to abide by the terms of the paid license agreement, which may include additional provisions and usage rights. Contact FileFlows for information on obtaining a paid license for commercial use.

# **Data Collection and Telemetry**

FileFlows collects telemetry data from your use of the Software. This data is anonymous and general in scope, and may include but is not limited to:

- Flow elements being used
- Scripts being used
- Number of files processed
- Number of files failed processing
- Database type
- Version installed
- Operating system
- Architecture (e.g., ARM, x64, etc.)
- Number of processing nodes
- Number of file runners
- Storage saved
- Flow templates being used
- Library templates being used
- DockerMods being used

This data is collected to improve the performance, features, and usability of the Software. By accepting this EULA, you consent to the collection of such telemetry data.


# **Disclaimer Regarding Data Management**

FileFlows allows users to create custom flows to manage their files and folders, including actions that involve deleting specific files. While users have the freedom to design flows according to their preferences and requirements, FileFlows is not liable for any unintended data loss or damage resulting from the use of its services. Users are solely responsible for ensuring the accuracy and safety of their flows and for taking appropriate precautions to prevent accidental data loss. FileFlows encourages users to review their flows carefully before execution and to back up their important data regularly to mitigate the risk of data loss.

# **Intellectual Property and Ownership**

FileFlows shall at all times retain ownership of the Software as originally downloaded by you and all subsequent downloads of the Software by you. The Software (and the copyright, and other intellectual property rights of whatever nature in the Software, including any modifications made thereto) are and shall remain the property of FileFlows.

FileFlows reserves the right to grant licenses to use the Software to third parties.

# **Termination**

This EULA agreement is effective from the date you first use the Software and shall continue until terminated. You may terminate it at any time upon written notice to FileFlows.

It will also terminate immediately if you fail to comply with any term of this EULA agreement. Upon such termination, the licenses granted by this EULA agreement will immediately terminate and you agree to stop all access and use of the Software. The provisions that by their nature continue and survive will survive any termination of this EULA agreement.

# **Governing Law**

This EULA agreement, and any dispute arising out of or in connection with this EULA agreement, shall be governed by and construed in accordance with the laws of New Zealand.

".Trim();
}