Attribute VB_Name = "modTime"
Option Explicit
Option Private Module

Private Declare Function timeGetTime Lib "winmm.dll" () As Long
Public lngStartTime As Long   'time in msec

Public Sub StartTimer()
    lngStartTime = timeGetTime()
End Sub
Public Function EndTimer() As Double
    EndTimer = timeGetTime() - lngStartTime
End Function
Public Sub delay(msdelay As Long)
   ' creates delay in ms
   Dim Temp As Double
   StartTimer
   Do Until EndTimer > (msdelay)
   Loop
End Sub

