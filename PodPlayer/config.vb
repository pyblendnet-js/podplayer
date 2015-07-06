Imports System.Xml  ' config load

Public Class XmlNodeWalkerClass
    Protected xDoc As XmlDocument
    Protected Nodes As XmlNodeList
    Protected NodeName As String
    Protected nodeIndex As Integer
    Protected Node As XmlNode
    Private ChildNodeWalker As XmlNodeWalkerClass = Nothing

    Public Sub New()
        nodeIndex = 0
    End Sub

    Public Sub New(ByRef xd As XmlDocument, ByRef nodelist As XmlNodeList, ByRef name As String)
        xDoc = xd
        Nodes = nodelist
        NodeName = name
        nodeIndex = 0
    End Sub

    Public Sub getStringConfig(ByVal arg_name As String, ByRef str As String)
        Dim reply = Node.Attributes.GetNamedItem(arg_name)
        If reply Is Nothing Then
            str = Nothing
            Return
            'Console.WriteLine("ERROR:Output in {0} is missing key tag", config_fid)
        End If
        str = reply.Value
    End Sub

    Public Sub getIntegerConfig(ByVal arg_name As String, ByRef int As Integer)
        Dim reply = Node.Attributes.GetNamedItem(arg_name)
        If reply Is Nothing Then
            Return
            'Console.WriteLine("ERROR:Output in {0} is missing key tag", config_fid)
        End If
        int = CInt(reply.Value)
    End Sub

    Public Sub getLineObjectConfig(ByRef line As Line)
        getIntegerConfig("x1", line.X1)
        getIntegerConfig("y1", line.Y1)
        getIntegerConfig("x2", line.X2)
        getIntegerConfig("y2", line.Y2)
    End Sub

    Public Sub getMarginObjectConfig(ByRef margin As Thickness)
        getIntegerConfig("x1", margin.Left)
        getIntegerConfig("y1", margin.Top)
        getIntegerConfig("x2", margin.Right)
        getIntegerConfig("y2", margin.Bottom)
    End Sub

    Public Sub getSizeObjectConfig(ByRef size As Size)
        Dim reply = Node.Attributes.GetNamedItem("size")
        If reply Is Nothing Then
            Return
            'Console.WriteLine("ERROR:Output in {0} is missing key tag", config_fid)
        End If
        Dim vals = reply.Value.Split(",")
        If vals.Count <> 2 Then
            Return
        End If
        size = New Size(CDbl(vals(0)), CDbl(vals(1)))
    End Sub

    Public Sub getRectObjectConfig(ByRef rect As Rect)
        Dim reply = Node.Attributes.GetNamedItem("rect")
        If reply Is Nothing Then
            Return
            'Console.WriteLine("ERROR:Output in {0} is missing key tag", config_fid)
        End If
        Dim vals = reply.Value.Split(",")
        If vals.Count <> 4 Then
            Return
        End If
        rect = New Rect(CDbl(vals(0)), CDbl(vals(1)), CDbl(vals(2)), CDbl(vals(3)))
    End Sub

    Public Sub reset()
        nodeIndex = 0
    End Sub

    Public Function nextNode() As Boolean
        Do
            If nodeIndex >= Nodes.Count Then
                Return False
            End If
            Node = Nodes(nodeIndex)
            nodeIndex += 1
        Loop Until (NodeName Is Nothing) Or (Node.Name = NodeName)  'for child nodes it is necessary to check the node name if filtered
        Return True
    End Function

    Public Function getChildNodes(ByRef name As String) As XmlNodeWalkerClass
        If Node.HasChildNodes() Then
            ChildNodeWalker = New XmlNodeWalkerClass(xDoc, Node.ChildNodes, name)
            Return ChildNodeWalker
        Else
            Return Nothing
        End If
    End Function

    Public Function makeChildNodes(ByRef name As String) As XmlNodeWalkerClass
        ChildNodeWalker = New XmlNodeWalkerClass(xDoc, Node.ChildNodes, name)
        Return ChildNodeWalker
    End Function

    Public Sub addChildNode(ByRef element As XmlElement)
        Node.AppendChild(element)
    End Sub

    Public Sub setAttributes(ByRef attribs As XmlAttributeCollection, ByVal argStrs As String(), ByVal args As Object(), ByVal default_args As Object())
        ' write attributes
        Dim i As Integer = 0
        For Each argstr In argStrs
            Dim attribute = xDoc.CreateAttribute(argstr)
            If i < default_args.Count Then
                If args(i) = default_args(i) Then  'if this is the default value then there is no reason to add it
                    i += 1
                    Continue For
                End If
            End If
            Dim r As Rect
            If args(i).GetType() = r.GetType Then
                'Dim rect_str As String = args(i).X & "," & args(i).Y & "," & args(i).Width & "," & args(i).Height
                Dim rect_str As String = formatRect(args(i))
                attribute.Value = rect_str
            Else
                attribute.Value = args(i)
            End If
            attribs.Append(attribute)
            i += 1
        Next
    End Sub

    Public Function formatRect(ByVal rect As Rect) As String 'limits to 2 decimal places
        Return Format(rect.X, "0.##") & "," & Format(rect.Y, "0.##") & "," & Format(rect.Width, "0.##") & "," & Format(rect.Height, "0.##")
    End Function

    Public Function makeElement(ByVal node_type As String, ByVal argStrs As String(), ByVal args As Object(), ByVal default_args As Object()) As XmlElement
        Dim element As XmlElement = xDoc.CreateElement(node_type)  'DocumentElement
        setAttributes(element.Attributes, argStrs, args, default_args)
        Return element
    End Function

    Public Function makeLineElement(ByRef line As Line) As XmlElement
        Return makeElement("Line", {"key", "x1", "y1", "x2", "y2"}, {line.Name, line.X1, line.Y1, line.X2, line.Y2}, {})
    End Function

    Public Function makeMarginElement(ByRef nm As String, ByRef t As Thickness) As XmlElement
        Return makeElement("margin", {"key", "x1", "y1", "x2", "y2"}, {nm, t.Left, t.Top, t.Right, t.Bottom}, {})
    End Function

    'makeSize and makeRect don't appear to be necessary
    'Public Function makeSizeElement(ByRef nm As String, ByRef size As Size) As XmlElement
    '    Dim size_str As String = size.Width & "," & size.Height
    '    Return makeElement(nm, {"size"}, {size_str}, {})
    'End Function

    'Public Function makeRectElement(ByRef nm As String, ByRef rect As Rect) As XmlElement
    '    Dim rect_str As String = rect.X & "," & rect.Y & "," & rect.Width & "," & rect.Height
    '    Return makeElement(nm, {"rect"}, {rect_str}, {})
    'End Function

End Class

Public Class XmlConfigClass
    Inherits XmlNodeWalkerClass

    Private xmlFid As String
    Private Head As XmlElement
    Private HeadAttributes As XmlAttributeCollection
    Private NodeWalker As XmlNodeWalkerClass
    Private nodeKey As String

    'for save

    Public Function getHeadAttribute(ByVal aname As String) As String
        Dim reply = HeadAttributes.GetNamedItem(aname)
        If reply Is Nothing Then
            Return Nothing
        End If
        Return reply.Value
    End Function

    Public Function loadDocument(ByVal fid As String) As String
        xmlFid = fid
        xDoc = New XmlDocument()
        Try
            xDoc.Load(fid)
            Head = xDoc.DocumentElement
            Console.WriteLine(Head.Name)
            HeadAttributes = Head.Attributes
            Return Head.Name
        Catch e As Exception
            Console.WriteLine("Cannot configeration file:" & fid)
            Console.WriteLine(e.Message)
        End Try
        Return Nothing
    End Function

    Public Function loadNodes(ByVal node_name As String) As Boolean
        ' fetch nodes - if not found then return false 
        Nodes = Head.GetElementsByTagName(node_name)
        NodeName = node_name
        Return (Not Nodes Is Nothing)
    End Function

    Public Sub setHeadAttributes(ByVal argStrs As String(), ByVal args As Object(), ByVal default_args As Object())
        setAttributes(Head.Attributes, argStrs, args, default_args)
    End Sub

    Public Sub createDocument(ByVal head_name As String)
        'load from file if available
        xDoc = New XmlDocument()
        '       Try
        Dim XmlProc As XmlDeclaration
        XmlProc = xDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes")
        xDoc.AppendChild(XmlProc)
        Head = xDoc.CreateElement(head_name)  'DocumentElement
    End Sub

    Public Sub addNode(ByRef element As XmlElement)
        Head.AppendChild(element)
        Node = Head.LastChild
    End Sub

    Public Sub saveDocument(ByVal fid As String)
        'use create document to begin and addNode to load
        xDoc.AppendChild(Head)
        Dim settings = New XmlWriterSettings()
        'settings.NewLineOnAttributes = True
        settings.Indent = True
        Dim output = XmlWriter.Create(fid, settings)
        xDoc.WriteTo(output)
        output.Close()
    End Sub


End Class
