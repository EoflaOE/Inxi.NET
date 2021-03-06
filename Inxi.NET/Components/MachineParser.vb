﻿
'    Inxi.NET  Copyright (C) 2020-2021  EoflaOE
'
'    This file is part of Inxi.NET
'
'    Inxi.NET is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    Inxi.NET is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with this program.  If not, see <https://www.gnu.org/licenses/>.

Imports System.Management
Imports Extensification.External.Newtonsoft.Json.JPropertyExts
Imports Claunia.PropertyList
Imports Newtonsoft.Json.Linq

Module MachineParser

    ''' <summary>
    ''' Parses machine info
    ''' </summary>
    ''' <param name="InxiToken">Inxi JSON token. Ignored in Windows.</param>
    Function ParseMachine(InxiToken As JToken, SystemProfilerToken As NSArray) As MachineInfo
        Dim MachInfo As MachineInfo
        If IsUnix() Then
            If IsMacOS() Then
                'Check for data type
                Debug("Checking for data type...")
                For Each DataType As NSDictionary In SystemProfilerToken
                    If DataType("_dataType").ToObject = "SPHardwareDataType" Then
                        Debug("DataType found: SPHardwareDataType...")

                        'Get information of a machine
                        Dim SoftwareEnum As NSArray = DataType("_items")
                        Debug("Enumerating machines...")
                        For Each SoftwareDict As NSDictionary In SoftwareEnum
                            'Get information of machine
                            Dim Type As String = If(SoftwareDict("machine_name").ToObject.ToString.Contains("MacBook"), "Laptop", "Desktop")
                            Dim MoboManufacturer As String = "Apple"
                            Dim MoboModel As String = SoftwareDict("machine_model").ToObject
                            Debug("Got information. Type: {0}, MoboManufacturer: {1}, MoboModel: {2}", Type, MoboManufacturer, MoboModel)

                            'Create an instance of machine class
                            MachInfo = New MachineInfo(Type, MoboManufacturer, MoboModel)
                        Next
                    End If
                Next
            Else
                Debug("Selecting the Machine token...")
                For Each InxiSys In InxiToken.SelectTokenKeyEndingWith("Machine")
                    'Get information of system
                    Dim Type As String = InxiSys.SelectTokenKeyEndingWith("Type")
                    Dim MoboManufacturer As String = InxiSys.SelectTokenKeyEndingWith("Mobo")
                    Dim MoboModel As String = InxiSys.SelectTokenKeyEndingWith("model")
                    Debug("Got information. Type: {0}, MoboManufacturer: {1}, MoboModel: {2}", Type, MoboManufacturer, MoboModel)

                    'Create an instance of system class
                    MachInfo = New MachineInfo(Type, MoboManufacturer, MoboModel)
                Next
            End If
        Else
            Debug("Selecting entries from Win32_ComputerSystem...")
            Dim WMIMachine As New ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")
            Debug("Selecting entries from Win32_BaseBoard...")
            Dim WMIBoard As New ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard")
            Debug("Selecting entries from Win32_OperatingSystem...")
            Dim WMISystem As New ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem")

            'Get information of system and motherboard
            Dim [Type] As String = ""
            Dim MoboModel As String = ""
            Dim MoboManufacturer As String = ""
            Debug("Getting the base objects...")
            For Each WMISystemBase As ManagementBaseObject In WMISystem.Get
                If WMISystemBase("Version").StartsWith("10") And Environment.OSVersion.Platform = PlatformID.Win32NT Then 'If running on Windows 10
                    Debug("Target is running Windows 10.")
                    For Each MachineBase As ManagementBaseObject In WMIMachine.Get
                        [Type] = MachineBase("ChassisSKUNumber")
                    Next
                End If
            Next
            Debug("Getting the base objects...")
            For Each MoboBase As ManagementBaseObject In WMIBoard.Get
                MoboModel = MoboBase("Model")
                MoboManufacturer = MoboBase("Manufacturer")
            Next
            Debug("Got information. Type: {0}, MoboManufacturer: {1}, MoboModel: {2}", [Type], MoboManufacturer, MoboModel)

            'Create an instance of system class
            MachInfo = New MachineInfo(Type, MoboManufacturer, MoboModel)
        End If

#Disable Warning BC42104
        Return MachInfo
#Enable Warning BC42104
    End Function

End Module
