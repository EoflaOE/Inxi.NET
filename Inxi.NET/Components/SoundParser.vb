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

Imports Extensification.DictionaryExts
Imports Extensification.External.Newtonsoft.Json.JPropertyExts
Imports System.Management
Imports Newtonsoft.Json.Linq
Imports Claunia.PropertyList

Module SoundParser

    ''' <summary>
    ''' Parses sound cards
    ''' </summary>
    ''' <param name="InxiToken">Inxi JSON token. Ignored in Windows.</param>
    Function ParseSound(InxiToken As JToken, SystemProfilerToken As NSArray) As Dictionary(Of String, Sound)
        Dim SPUParsed As New Dictionary(Of String, Sound)
        Dim SPU As Sound

        'SPU information fields
        Dim SPUName As String
        Dim SPUVendor As String
        Dim SPUDriver As String

        If IsUnix() Then
            If IsMacOS() Then
                'TODO: Currently, Inxi.NET adds a dumb device to parsed device. We need actual data. Use "system_profiler SPAudioDataType -xml >> audio.plist" and attach it to Issues
                'Create an instance of sound class
                Debug("TODO: Currently, Inxi.NET adds a dumb device to parsed device. We need actual data. Use ""system_profiler SPAudioDataType -xml >> audio.plist"" and attach it to Issues.")
                SPU = New Sound("Placeholder", "EoflaOE", "SoundParser")
                SPUParsed.AddIfNotFound("Placeholder", SPU)
                Debug("Added Placeholder to the list of parsed SPUs.")
            Else
                Debug("Selecting the Audio token...")
                For Each InxiSPU In InxiToken.SelectTokenKeyEndingWith("Audio")
                    If InxiSPU.SelectTokenKeyEndingWith("Device") IsNot Nothing Then
                        'Get information of a sound card
                        SPUName = InxiSPU.SelectTokenKeyEndingWith("Device")
                        SPUVendor = InxiSPU.SelectTokenKeyEndingWith("vendor")
                        SPUDriver = InxiSPU.SelectTokenKeyEndingWith("driver")
                        Debug("Got information. SPUName: {0}, SPUDriver: {1}, SPUVendor: {2}", SPUName, SPUDriver, SPUVendor)

                        'Create an instance of sound class
                        SPU = New Sound(SPUName, SPUVendor, SPUDriver)
                        SPUParsed.AddIfNotFound(SPUName, SPU)
                        Debug("Added {0} to the list of parsed SPUs.", SPUName)
                    End If
                Next
            End If
        Else
            Debug("Selecting entries from Win32_SoundDevice...")
            Dim SoundDevice As New ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice")

            'TODO: Driver not implemented in Windows
            'Get information of sound cards
            Debug("Getting the base objects...")
            Debug("TODO: Driver not implemented in Windows.")
            For Each Device As ManagementBaseObject In SoundDevice.Get
                'Get information of a sound card
                SPUName = Device("ProductName")
                SPUVendor = Device("Manufacturer")
                SPUDriver = ""
                Debug("Got information. SPUName: {0}, SPUDriver: {1}, SPUVendor: {2}", SPUName, SPUDriver, SPUVendor)

                'Create an instance of sound class
                SPU = New Sound(SPUName, SPUVendor, SPUDriver)
                SPUParsed.AddIfNotFound(SPUName, SPU)
                Debug("Added {0} to the list of parsed SPUs.", SPUName)
            Next
        End If

        Return SPUParsed
    End Function

End Module
