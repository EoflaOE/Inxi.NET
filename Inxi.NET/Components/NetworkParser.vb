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

Module NetworkParser

    ''' <summary>
    ''' Parses network cards
    ''' </summary>
    ''' <param name="InxiToken">Inxi JSON token. Ignored in Windows.</param>
    Function ParseNetwork(InxiToken As JToken, SystemProfilerToken As NSArray) As Dictionary(Of String, Network)
        Dim NetworkParsed As New Dictionary(Of String, Network)
        Dim Network As Network
        Dim NetworkCycled As Boolean

        'Network information fields
        Dim NetName As String = ""
        Dim NetDriver As String = ""
        Dim NetDriverVersion As String = ""
        Dim NetDuplex As String = ""
        Dim NetSpeed As String = ""
        Dim NetState As String = ""
        Dim NetMacAddress As String = ""
        Dim NetDeviceID As String = ""

        If IsUnix() Then
            If IsMacOS() Then
                'TODO: Name, Driver, DriverVersion, and State not implemented in macOS.
                'Check for data type
                Debug("Checking for data type...")
                Debug("TODO: Name, Driver, DriverVersion, and State not implemented in macOS.")
                For Each DataType As NSDictionary In SystemProfilerToken
                    If DataType("_dataType").ToObject = "SPNetworkDataType" Then
                        Debug("DataType found: SPNetworkDataType...")

                        'Get information of a network adapter
                        Dim NetEnum As NSArray = DataType("_items")
                        Debug("Enumerating network cards...")
                        For Each NetDict As NSDictionary In NetEnum
                            Dim EthernetDict As NSDictionary = NetDict("Ethernet")
                            Dim EthernetMediaOptions As NSArray = EthernetDict("MediaOptions")
                            For Each MediaOption As NSObject In EthernetMediaOptions
                                NetDuplex += MediaOption.ToObject
                            Next
                            NetSpeed = EthernetDict("MediaSubType").ToObject
                            NetMacAddress = EthernetDict("MAC Address").ToObject
                            NetDeviceID = NetDict("interface").ToObject
                            Debug("Got information. NetName: {0}, NetDriver: {1}, NetDriverVersion: {2}, NetDuplex: {3}, NetSpeed: {4}, NetState: {5}, NetDeviceID: {6}", NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetDeviceID)

                            'Create instance of network class
                            Network = New Network(NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetMacAddress, NetDeviceID)
                            NetworkParsed.Add(NetName, Network)
                            Debug("Added {0} to the list of parsed network cards.", NetName)
                            NetDuplex = ""
                            NetSpeed = ""
                            NetMacAddress = ""
                            NetDeviceID = ""
                        Next
                    End If
                Next
            Else
                Debug("Selecting the Network token...")
                For Each InxiNetwork In InxiToken.SelectTokenKeyEndingWith("Network")
                    'Get information of a network card
                    If InxiNetwork.SelectTokenKeyEndingWith("Device") IsNot Nothing Then
                        NetName = InxiNetwork.SelectTokenKeyEndingWith("Device")
                        If InxiNetwork.SelectTokenKeyEndingWith("type") IsNot Nothing And InxiNetwork.SelectTokenKeyEndingWith("type") = "network bridge" Then
                            NetDriver = InxiNetwork.SelectTokenKeyEndingWith("driver")
                            NetDriverVersion = InxiNetwork.SelectTokenKeyEndingWith("v")
                            NetworkCycled = True
                        Else
                            NetDriver = InxiNetwork.SelectTokenKeyEndingWith("driver")
                            NetDriverVersion = InxiNetwork.SelectTokenKeyEndingWith("v")
                        End If
                    ElseIf InxiNetwork.SelectTokenKeyEndingWith("IF") IsNot Nothing Then
                        NetDuplex = InxiNetwork.SelectTokenKeyEndingWith("duplex")
                        NetSpeed = InxiNetwork.SelectTokenKeyEndingWith("speed")
                        NetState = InxiNetwork.SelectTokenKeyEndingWith("state")
                        NetMacAddress = InxiNetwork.SelectTokenKeyEndingWith("mac")
                        NetDeviceID = InxiNetwork.SelectTokenKeyEndingWith("IF")
                        NetworkCycled = True 'Ensures that all info is filled.
                    End If

                    'Create instance of network class
                    If NetworkCycled Then
                        Debug("Got information. NetName: {0}, NetDriver: {1}, NetDriverVersion: {2}, NetDuplex: {3}, NetSpeed: {4}, NetState: {5}, NetDeviceID: {6}", NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetDeviceID)
                        Network = New Network(NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetMacAddress, NetDeviceID)
                        NetworkParsed.Add(NetName, Network)
                        Debug("Added {0} to the list of parsed network cards.", NetName)
                        NetName = ""
                        NetDriver = ""
                        NetDriverVersion = ""
                        NetDuplex = ""
                        NetSpeed = ""
                        NetState = ""
                        NetMacAddress = ""
                        NetDeviceID = ""
                        NetworkCycled = False
                    End If
                Next
            End If
        Else
            Debug("Selecting entries from Win32_NetworkAdapter...")
            Dim Networks As New ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter")
            Debug("Selecting entries from Win32_PnPSignedDriver with device class of 'NET'...")
            Dim NetworkDrivers As New ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver WHERE DeviceClass='NET'")

            'TODO: Network driver duplex not implemented in Windows
            'Get information of network cards
            Debug("Getting the base objects...")
            Debug("TODO: Network driver duplex not implemented in Windows")
            For Each Networking As ManagementBaseObject In Networks.Get
                'Get information of a network card
                NetName = Networking("Name")
                NetDriver = Networking("ServiceName")
                NetSpeed = Networking("Speed")
                NetState = Networking("NetConnectionStatus")
                NetMacAddress = Networking("MACAddress")
                NetDeviceID = Networking("DeviceID")
                For Each NetworkDriver As ManagementBaseObject In NetworkDrivers.Get
                    If NetworkDriver("Description") = NetName Then
                        NetDriverVersion = NetworkDriver("DriverVersion")
                        Exit For
                    End If
                Next
                Debug("Got information. NetName: {0}, NetDriver: {1}, NetDriverVersion: {2}, NetDuplex: {3}, NetSpeed: {4}, NetState: {5}, NetDeviceID: {6}", NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetDeviceID)

                'Create instance of network class
                Network = New Network(NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetMacAddress, NetDeviceID)
                NetworkParsed.AddIfNotFound(NetName, Network)
                Debug("Added {0} to the list of parsed network cards.", NetName)
            Next
        End If

        'Return list of network devices
        Return NetworkParsed
    End Function

End Module
