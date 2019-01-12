Attribute VB_Name = "NONFMT"
Option Explicit

'' """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
''  Copyright � 1999 - 2001 Agilent Technologies Inc.  All rights reserved.
''
'' You have a royalty-free right to use, modify, reproduce and distribute
'' the Sample Application Files (and/or any modified version) in any way
'' you find useful, provided that you agree that Agilent Technologies has no
'' warranty, obligations or liability for any Sample Application Files.
''
'' Agilent Technologies provides programming examples for illustration only,
'' This sample program assumes that you are familiar with the programming
'' language being demonstrated and the tools used to create and debug
'' procedures. Agilent Technologies support engineers can help explain the
'' functionality of Agilent Technologies software components and associated
'' commands, but they will not modify these samples to provide added
'' functionality or construct procedures to meet your specific needs.
'' """"""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""

'' To develop VISA applications in Microsoft Visual Basic, you first need
'' to add the Visual Basic (VB) declaration file in your VB project as a
'' Module. This file contains the VISA function definitions and constant
'' declarations needed to make VISA calls from Visual Basic.
'' To add this module to your project in VB 6, from the menu, select
'' Project->Add Module, select the 'Existing' tab, and browse to the
'' directory containing the VB Declaration file, select visa32.bas, and
'' press 'Open'.
''
'' The name and location of the VB declaration file depends on which
'' operating system you are using.  Assuming the 'standard' VISA directory
'' of C:\Program Files\Visa or the 'standard' VXIpnp directory of
'' C:\VXIpnp, the visa32.bas file can be located in one of the following:
''
''   \winnt\agvisa\include\visa32.bas - Windows NT/2000
''   \winnt\include\visa32.bas        - Windows NT/2000
''   \win95\include\visa32.bas        - Windows 95/98/Me

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'  nonfmt.bas
'  This example program illustrates using non-formatted I/O
'  functions to communicate with a GPIB device. The following
'  subroutine sends a '*IDN?' query to an instrument and reads
'  the result into a string using viRead and viWrite calls.
'  Note that you may have to change the address for your
'  device from "22"
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Sub Main()
   Dim status As Long
   Dim defrm As Long
   Dim vi As Long
   Dim strRes As String * 200
   Dim actual As Long

   On Error GoTo ErrorHandler

   ' Open the default resource manager session
   status = viOpenDefaultRM(defrm)
   If (status < VI_SUCCESS) Then GoTo VisaErrorHandler

   ' Open the session to the resource
   ' The "GPIB0" parameter is the VISA Interface name to
   ' an GPIB instrument as defined in:
   '    Start->Programs->Agilent IO Libraries->IO Config
   ' Change this name to whatever you have defined for your
   ' VISA Interface.
   ' "GPIB0::22::INSTR" is the address string for the device -
   ' this address will be the same as seen in:
   '     Start->Programs->Agilent IO Libraries->VISA Assistant
   ' after the VISA Interface Name is defined in IO Config

   status = viOpen(defrm, "GPIB0::22::INSTR", 0, 0, vi)
   If (status < VI_SUCCESS) Then GoTo VisaErrorHandler

   ' Ask for the device's *IDN string.
   status = viWrite(vi, "*IDN?" + Chr$(10), 6, actual)
   If (status < VI_SUCCESS) Then GoTo VisaErrorHandler

   ' Read the results
   Call viRead(vi, strRes, 200, actual)

   ' Display the results
   MsgBox Left$(strRes, actual), , "*IDN? Result"

   ' Close the resource manager session (which closes everything)
   Call viClose(defrm)

   Exit Sub

ErrorHandler:
   ' Display the error message
   MsgBox "*** Error : " + Error$, vbExclamation
   Exit Sub


VisaErrorHandler:
   Dim strVisaErr As String * 200
   Call viStatusDesc(defrm, status, strVisaErr)
   MsgBox "*** Error : " & strVisaErr, vbExclamation, "VISA Error Message"
   Exit Sub

End Sub

