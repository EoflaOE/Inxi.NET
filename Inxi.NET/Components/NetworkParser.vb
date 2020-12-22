﻿
'    Inxi.NET  Copyright (C) 2020  EoflaOE
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
Imports System.Management
Imports Newtonsoft.Json.Linq

Module NetworkParser

    ''' <summary>
    ''' Parses network cards
    ''' </summary>
    ''' <param name="InxiToken">Inxi JSON token. Ignored in Windows.</param>
    Function ParseNetwork(InxiToken As JToken) As Dictionary(Of String, Network)
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
            For Each InxiNetwork In InxiToken.SelectToken("006#Network")
                If InxiNetwork("001#Device") IsNot Nothing Then
                    'Get information of a network card
                    NetName = InxiNetwork("001#Device")
                    If InxiNetwork("002#type") IsNot Nothing And InxiNetwork("002#type") = "network bridge" Then
                        NetDriver = InxiNetwork("003#driver")
                        NetDriverVersion = InxiNetwork("004#v")
                        NetworkCycled = True
                    Else
                        NetDriver = InxiNetwork("002#driver")
                        NetDriverVersion = InxiNetwork("003#v")
                    End If
                ElseIf InxiNetwork("000#IF") IsNot Nothing Then
                    NetDuplex = InxiNetwork("003#duplex")
                    NetSpeed = InxiNetwork("002#speed")
                    NetState = InxiNetwork("001#state")
                    NetMacAddress = InxiNetwork("004#mac")
                    NetDeviceID = InxiNetwork("000#IF")
                    NetworkCycled = True 'Ensures that all info is filled.
                End If

                'Create instance of network class
                If NetworkCycled Then
                    Network = New Network(NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetMacAddress, NetDeviceID)
                    NetworkParsed.Add(NetName, Network)
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
        Else
            Dim Networks As New ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter")
            For Each Networking As ManagementBaseObject In Networks.Get
                'TODO: Network driver version and duplex not implemented in Windows
                NetName = Networking("Name")
                NetDriver = Networking("ServiceName")
                NetSpeed = Networking("Speed")
                NetState = Networking("NetConnectionStatus")
                NetMacAddress = Networking("MACAddress")
                NetDeviceID = Networking("DeviceID")
                Network = New Network(NetName, NetDriver, NetDriverVersion, NetDuplex, NetSpeed, NetState, NetMacAddress, NetDeviceID)
                NetworkParsed.AddIfNotFound(NetName, Network)
            Next
        End If

        'Return list of network devices
        Return NetworkParsed
    End Function

End Module