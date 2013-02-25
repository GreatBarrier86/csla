Imports System.Reflection
Imports System.Security.Principal
Imports System.Configuration
Imports System.Collections.Specialized

''' <summary>
''' 
''' </summary>
Namespace Server

  ''' <summary>
  ''' Implements the server-side DataPortal as discussed
  ''' in Chapter 5.
  ''' </summary>
  Public Class DataPortal
    Inherits MarshalByRefObject

#Region " Data Access "

    ''' <summary>
    ''' Called by the client-side DataPortal to create a new object.
    ''' </summary>
    ''' <param name="Criteria">Object-specific criteria.</param>
    ''' <param name="Principal">The user's principal object (if using CSLA .NET security).</param>
    ''' <returns>A populated business object.</returns>
    Public Function Create(ByVal Criteria As Object, ByVal context As DataPortalContext) As Object

      Dim obj As Object

      Try
        SetContext(context)

        ' create an instance of the business object
        obj = CreateBusinessObject(Criteria)

        ' tell the business object we're about to make a DataPortal_xyz call
        CallMethod(obj, "DataPortal_OnDataPortalInvoke", New DataPortalEventArgs(context))

        ' tell the business object to fetch its data
        CallMethod(obj, "DataPortal_Create", Criteria)

        ' tell the business object the DataPortal_xyz call is complete
        CallMethod(obj, "DataPortal_OnDataPortalInvokeComplete", New DataPortalEventArgs(context))

        ' return the populated business object as a result
        If context.IsRemotePortal Then
          Serialization.SerializationNotification.OnSerializing(obj)
        End If

        Dim result As New DataPortalResult(obj)
        ClearContext(context)
        Return result

      Catch ex As Exception
        Dim result As New DataPortalResult(obj)
        ClearContext(context)
        Throw New DataPortalException("DataPortal.Create " & GetResourceString("FailedOnServer"), ex, result)
      End Try

    End Function

    ''' <summary>
    ''' Called by the client-side DataProtal to retrieve an object.
    ''' </summary>
    ''' <param name="Criteria">Object-specific criteria.</param>
    ''' <param name="Principal">The user's principal object (if using CSLA .NET security).</param>
    ''' <returns>A populated business object.</returns>
    Public Function Fetch(ByVal Criteria As Object, ByVal context As DataPortalContext) As Object

      Dim obj As Object

      Try
        SetContext(context)

        ' create an instance of the business object
        obj = CreateBusinessObject(Criteria)

        ' tell the business object we're about to make a DataPortal_xyz call
        CallMethod(obj, "DataPortal_OnDataPortalInvoke", New DataPortalEventArgs(context))

        ' tell the business object to fetch its data
        CallMethod(obj, "DataPortal_Fetch", Criteria)

        ' tell the business object the DataPortal_xyz call is complete
        CallMethod(obj, "DataPortal_OnDataPortalInvokeComplete", New DataPortalEventArgs(context))

        ' return the populated business object as a result
        If context.IsRemotePortal Then
          Serialization.SerializationNotification.OnSerializing(obj)
        End If

        Dim result As New DataPortalResult(obj)
        ClearContext(context)
        Return result

      Catch ex As Exception
        Dim result As New DataPortalResult(obj)
        ClearContext(context)
        Throw New DataPortalException("DataPortal.Fetch " & GetResourceString("FailedOnServer"), ex, result)
      End Try

    End Function

    ''' <summary>
    ''' Called by the client-side DataPortal to update an object.
    ''' </summary>
    ''' <param name="obj">A reference to the object being updated.</param>
    ''' <param name="Principal">The user's principal object (if using CSLA .NET security).</param>
    ''' <returns>A reference to the newly updated object.</returns>
    Public Function Update(ByVal obj As Object, ByVal context As DataPortalContext) As Object

      Try
        SetContext(context)

        If context.IsRemotePortal Then
          Serialization.SerializationNotification.OnDeserialized(obj)
        End If

        ' tell the business object we're about to make a DataPortal_xyz call
        CallMethod(obj, "DataPortal_OnDataPortalInvoke", New DataPortalEventArgs(context))

        ' tell the business object to update itself
        CallMethod(obj, "DataPortal_Update")

        ' tell the business object the DataPortal_xyz call is complete
        CallMethod(obj, "DataPortal_OnDataPortalInvokeComplete", New DataPortalEventArgs(context))

        If context.IsRemotePortal Then
          Serialization.SerializationNotification.OnSerializing(obj)
        End If

        Dim result As New DataPortalResult(obj)
        ClearContext(context)
        Return result

      Catch ex As Exception
        Dim result As New DataPortalResult(obj)
        ClearContext(context)
        Throw New DataPortalException("DataPortal.Update " & GetResourceString("FailedOnServer"), ex, result)
      End Try

    End Function

    ''' <summary>
    ''' Called by the client-side DataPortal to delete an object.
    ''' </summary>
    ''' <param name="Criteria">Object-specific criteria.</param>
    ''' <param name="Principal">The user's principal object (if using CSLA .NET security).</param>
    Public Function Delete(ByVal Criteria As Object, ByVal context As DataPortalContext) As Object

      Dim obj As Object

      Try
        SetContext(context)

        ' create an instance of the business object
        obj = CreateBusinessObject(Criteria)

        ' tell the business object we're about to make a DataPortal_xyz call
        CallMethod(obj, "DataPortal_OnDataPortalInvoke", New DataPortalEventArgs(context))

        ' tell the business object to delete itself
        CallMethod(obj, "DataPortal_Delete", Criteria)

        ' tell the business object the DataPortal_xyz call is complete
        CallMethod(obj, "DataPortal_OnDataPortalInvokeComplete", New DataPortalEventArgs(context))

        Dim result As New DataPortalResult
        ClearContext(context)
        Return result

      Catch ex As Exception
        Dim result As New DataPortalResult
        ClearContext(context)
        Throw New DataPortalException("DataPortal.Delete " & GetResourceString("FailedOnServer"), ex, result)
      End Try

    End Function

#End Region

#Region " Security "

    Private Function AUTHENTICATION() As String

      Return ConfigurationSettings.AppSettings("Authentication")

    End Function

    Private Sub SetContext(ByVal context As DataPortalContext)

      ' if the dataportal is not remote then
      ' do nothing
      If Not context.IsRemotePortal Then Exit Sub

      Dim objPrincipal As IPrincipal
      Dim objIdentity As IIdentity

      ' set the app context to the value we got from the
      ' client
      CSLA.ApplicationContext.SetContext(context.ClientContext, context.GlobalContext)

      If AUTHENTICATION() = "Windows" Then
        ' When using integrated security, Principal must be Nothing 
        If context.Principal Is Nothing Then
          ' Set .NET to use integrated security 
          AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal)
          Exit Sub

        Else
          Throw New Security.SecurityException( _
            GetResourceString("NoPrincipalAllowedException"))
        End If
      End If

      ' We expect the Principal to be of the type BusinessPrincipal, but we can't enforce 
      ' that since it causes a circular reference with the business library. 
      ' Instead we must use type Object for the parameter, so here we do a check 
      ' on the type of the parameter. 
      objPrincipal = context.Principal
      If Not (objPrincipal Is Nothing) Then
        objIdentity = objPrincipal.Identity
        If Not (objIdentity Is Nothing) Then
          If objIdentity.AuthenticationType = "CSLA" Then
            ' See if our current principal is different from the caller's principal 
            If Not ReferenceEquals(context.Principal, _
                System.Threading.Thread.CurrentPrincipal) Then

              ' The caller had a different principal, so change ours to match the 
              ' caller's, so all our objects use the caller's security. 
              System.Threading.Thread.CurrentPrincipal = context.Principal
            End If

          Else
            Throw New Security.SecurityException( _
              GetResourceString("BusinessPrincipalException") & " " & CType(context.Principal, Object).ToString())
          End If

        End If

      Else
        Throw New Security.SecurityException( _
          GetResourceString("BusinessPrincipalException") & " Nothing")
      End If

    End Sub

    Private Sub ClearContext(ByVal context As DataPortalContext)

      ' if the dataportal is not remote then
      ' do nothing
      If Not context.IsRemotePortal Then Exit Sub

      ApplicationContext.Clear()

    End Sub

#End Region

#Region " Creating the business object "

    Private Function CreateBusinessObject(ByVal Criteria As Object) As Object

      Dim businessType As Type

      If Criteria.GetType.IsSubclassOf(GetType(CriteriaBase)) Then
        ' get the type of the actual business object
        ' from CriteriaBase (using the new scheme)
        businessType = CType(Criteria, CriteriaBase).ObjectType

      Else
        ' get the type of the actual business object
        ' based on the nested class scheme in the book
        businessType = Criteria.GetType.DeclaringType
      End If

      ' create an instance of the business object
      Return Activator.CreateInstance(businessType, True)

    End Function

#End Region

#Region " Calling a method "

    Private Function CallMethod(ByVal obj As Object, ByVal method As String, ByVal ParamArray params() As Object) As Object

      ' call a private method on the object
      Dim info As MethodInfo = GetMethod(obj.GetType, method)
      Dim result As Object

      Try
        result = info.Invoke(obj, params)

      Catch e As Exception
        Throw New CallMethodException(info.Name & " method call failed", e.InnerException)
      End Try
      Return result

    End Function

    Private Function GetMethod(ByVal ObjectType As Type, ByVal method As String) As MethodInfo

      Return ObjectType.GetMethod(method, _
        BindingFlags.FlattenHierarchy Or _
        BindingFlags.Instance Or _
        BindingFlags.Public Or _
        BindingFlags.NonPublic)

    End Function

#End Region

  End Class

End Namespace
