Imports System.IO
Imports System.Threading

Public Class FrmAsyncProgress
    ''' <summary>
    ''' https_://www.dreamincode.net/forums/topic/388819-asyncawait-with-progressbar-and-cancellation/
    ''' </summary>
    ''' 
    Private tokenSource As CancellationTokenSource

    Private Sub FrmAsyncProgress_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        barFileProgress.Minimum = 0
        barFileProgress.Maximum = 100
        btnCancel.Enabled = False
    End Sub

    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        If String.IsNullOrWhiteSpace(txtPath.Text) Then
            MessageBox.Show("Provide a location first.", "Location")
            Exit Sub
        End If
        Dim sLocation As String = txtPath.Text.Trim()
        If Not Directory.Exists(sLocation) Then
            MessageBox.Show("Directory doesn't exist.", "Location")
            Exit Sub
        End If

        Dim progressIndicator = New Progress(Of Integer)(AddressOf UpdateProgress)

        btnStart.Enabled = False
        btnCancel.Enabled = True
        lblPercent.Text = "0%"
        tokenSource = New CancellationTokenSource()

        Try
            Dim allFiles As Integer = Await AllSubfolderFiles(sLocation, progressIndicator, tokenSource.Token)
            Debug.WriteLine(allFiles.ToString())        'the number of subfolders iterated
        Catch ex As OperationCanceledException
            'do stuff when cancelled
            lblPercent.Text = "Cancelled"
        End Try

        btnStart.Enabled = True
        btnCancel.Enabled = False
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        btnCancel.Enabled = False
        btnStart.Enabled = False
        tokenSource.Cancel()
    End Sub

    Private Async Function AllSubfolderFiles(location As String, progress As IProgress(Of Integer),
                                             token As CancellationToken) As Task(Of Integer)
        Dim dirsTotal As Integer = Directory.GetDirectories(location).Length
        Dim dirsFraction As Integer = Await Task(Of Integer).Run(
            Function()
                Dim counter As Integer = 0
                For Each subDir As String In Directory.GetDirectories(location)
                    SubfolderFiles(subDir)
                    counter += 1
                    token.ThrowIfCancellationRequested()
                    If progress IsNot Nothing Then
                        progress.Report(counter * 100 / dirsTotal)
                    End If
                Next

                Return counter
            End Function
            )
        Return dirsFraction
    End Function

    Private Sub UpdateProgress(value As Integer)
        barFileProgress.Value = value
        lblPercent.Text = (value / 100).ToString("#0.##%")
    End Sub

    Private Sub SubfolderFiles(location As String)
        'source: http://stackoverflow.com/questions/16237291/visual-basic-2010-continue-on-error-unauthorizedaccessexception#answer-16237749

        Dim paths = New Queue(Of String)()
        Dim fileNames = New List(Of String)()

        paths.Enqueue(location)

        While paths.Count > 0
            Dim sDir = paths.Dequeue()

            Try
                Dim files = Directory.GetFiles(sDir)
                For Each file As String In Directory.GetFiles(sDir)
                    fileNames.Add(file)
                Next

                For Each subDir As String In Directory.GetDirectories(sDir)
                    paths.Enqueue(subDir)
                Next
            Catch ex As UnauthorizedAccessException
                ' log the exception or ignore it
                Debug.WriteLine("Directory {0}  could not be accessed!", sDir)
            Catch ex As PathTooLongException
                'bypass
                '**** handle it in the future!
            Catch ex As Exception
                ' log the exception or ...
                Throw
            End Try
        End While
        'could return fileNames collection
    End Sub


End Class
