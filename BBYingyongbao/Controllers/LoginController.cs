using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using BBYingyongbao.Common;
using BBYingyongbao.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BBYingyongbao.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {

        [HttpGet]
        [Route("test")]
        public JSendResponse<string> test()
        {
            return JSendResponse<string>.CreateSuccess("success");
        }

        [HttpPost]
        [Route("logout")]
        public JSendResponse<ERPResponseGeneral> Logout(string id)
        {
            DDUserModel user = JsonConvert.DeserializeObject<DDUserModel>(id);

            LoggerHelper.LogInfo(this.GetType(),user.ToDictionaryString(user));

            if (!ValidateLogoutRequestParameter(user)) {
                LoggerHelper.LogInfo(this.GetType(), "----the request parameter is not passing proper parameter: " +user.ToDictionaryString(user));
                return JSendResponse<ERPResponseGeneral>.CreateFail(user);
            }

            var ParameterDictionary = JsonFileReader.ReadTOJson("Content/Json/UserLogoutAPIPostString.json");
            UserInfoViewModel userModel = GetUserByCondition(user, ParameterDictionary, "IfExistUser");
            if (userModel != null && "1".Equals(userModel.returnStyle))
            {
                ERPResponseGeneral unbindResponse = UnbindUserFromDD(user, ParameterDictionary);
                if (unbindResponse != null && "1".Equals(unbindResponse.statusCode))
                {
                    return JSendResponse<ERPResponseGeneral>.CreateSuccess(unbindResponse);
                }
                else
                {
                    LoggerHelper.LogInfo(this.GetType(), "----UnbindUserFromDD is not returning status code 1,the status code is: " + unbindResponse.statusCode);
                    return JSendResponse<ERPResponseGeneral>.CreateFail(unbindResponse);
                }
            }
            LoggerHelper.LogInfo(this.GetType(), "----GetUserByCondition is not returning status code 1,the status code is: " + userModel.returnStyle);
            return JSendResponse<ERPResponseGeneral>.CreateFail(userModel);
        }

        [HttpPost]
        [Route("login")]
        public JSendResponse<UserInfoViewModel> Login(string id)
        {
            DDUserModel user = JsonConvert.DeserializeObject<DDUserModel>(id);
            LoggerHelper.LogInfo(this.GetType(), user.ToDictionaryString(user));

            try
            {
                if (!ValidateLoginByUsernameAndPassword(user)) {
                    LoggerHelper.LogInfo(this.GetType(), "----the request parameter is not passing proper parameter: " + user.ToDictionaryString(user));
                    return JSendResponse<UserInfoViewModel>.CreateFail(user);
                }
                var ParameterDictionary = JsonFileReader.ReadTOJson("Content/Json/UserLoginERPAPIPostString.json");

                UserInfoViewModel userModel = GetUserByCondition(user, ParameterDictionary, "IfExistUser");               
                UserInfoViewModel ifExistDDIdUser = GetUserByCondition(user,ParameterDictionary,"ifExistDDIdUser");

                if (userModel != null && "1".Equals(userModel.returnStyle))
                {
                    ERPResponseGeneral info = ChangeUserDDId(userModel, ParameterDictionary, user,ifExistDDIdUser);
                    if (info != null && "1".Equals(info.statusCode))
                    {
                        return JSendResponse<UserInfoViewModel>.CreateSuccess(userModel);
                    }
                    else
                    {
                        LoggerHelper.LogInfo(this.GetType(), "ChangeUserDDId is not returning status code 1,the user info:" + user.Username);
                        return JSendResponse<UserInfoViewModel>.CreateFail(info);
                    }
                }

                LoggerHelper.LogInfo(this.GetType(), "GetUserByUserIdAndPassword is not returning correct status code,the user info:" + user.Username);
                return JSendResponse<UserInfoViewModel>.CreateFail(userModel);
            }
            catch (Exception ex)
            {
                LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----Exception when LoginController post() is running", ex);
                return JSendResponse<UserInfoViewModel>.CreateError(ex.Message);
            }
        }

        [HttpGet]
        public JSendResponse<UserInfoViewModel> Get(string DDUserId)
        {
            if (string.IsNullOrEmpty(DDUserId)) {
                LoggerHelper.LogInfo(this.GetType(), "----the request parameter is not passing proper parameter: DDUserId:" +DDUserId );
                return JSendResponse<UserInfoViewModel>.CreateFail("DDUserId:" + DDUserId);
            }
            using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimData.aspx")))
            {
                try
                {
                    var requestParameterDictionary = JsonFileReader.ReadToDictionary("Content/Json/UserLoginERPAPIQueryString.json");

                    ProcessLoginRequestParameter(requestParameterDictionary, DDUserId);
                    HttpContent content = new FormUrlEncodedContent(requestParameterDictionary);

                    HttpResponseMessage response = client.PostAsync("http://erptest.bb-pco.com/KPIGetData/DimData.aspx", content).Result;
                    var result = response.Content.ReadAsStringAsync().Result;
                    JObject jobj;
                    ERPResponseGeneral generalInfo;
                    try
                    {
                        jobj = JObject.Parse(result);
                        generalInfo = new ERPResponseGeneral().serializeRequestDataResponse(jobj);
                    }
                    catch (Exception ex)
                    {
                        string parameter = string.Join(";", requestParameterDictionary.Select(x => x.Key + ":" + x.Value).ToArray());
                        LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----the client is returning unexpected result,the parameter is: " + parameter + "; the result is: " + result);
                        throw;
                    }

                    if ("1".Equals(generalInfo.statusCode))
                    {
                        UserInfoViewModel model = new UserInfoViewModel();
                        model.ToUserInfoViewModel(model, jobj["data"][0]);
                        model.returnStyle = "1";
                        return JSendResponse<UserInfoViewModel>.CreateSuccess(model);
                    }

                    LoggerHelper.LogInfo(this.GetType(), "----get usermodel by DDUserId is not returning status code 1,the status code is: " + generalInfo.statusCode);
                    return JSendResponse<UserInfoViewModel>.CreateFail(generalInfo);
                }
                catch (Exception ex)
                {
                    LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "---- Exception when Login Controller get is running");
                    return JSendResponse<UserInfoViewModel>.CreateError(ex.Message);
                }
            }
        }

        #region private section
        private bool ValidateLogoutRequestParameter(DDUserModel user) {
            if (string.IsNullOrEmpty(user.DDUserId) || string.IsNullOrEmpty(user.ERPUserId))
            {
                return false;
            }
            return true;
        }

        private bool ValidateGetUserByDDUserID(DDUserModel user) {
            if (string.IsNullOrEmpty(user.DDUserId)) {
                return false;
            }
            return true;
        }

        private bool ValidateLoginByUsernameAndPassword(DDUserModel user) {
            if (string.IsNullOrEmpty(user.DDUserId)||string.IsNullOrEmpty(user.Username)||string.IsNullOrEmpty(user.Password)) {
                return false;
            }
            return true;
        }
        private void ProcessClearDDIDParameter(Dictionary<string, string> list, string ChangedDDUserId)
        {
            try
            {
                list["TableID"] = ChangedDDUserId;
                list["rand"] = DateTime.Now.ToString("f");
                list.Add("Support14", "");
            }
            catch (Exception ex)
            {
                string parameter = string.Join(";", list.Select(x => x.Key + "=" + x.Value).ToArray());
                LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----error when process ProcessClearDDIDParameter,the parameter is: " + parameter);
                throw;
            }
        }

        private ERPResponseGeneral UnbindUserFromDD(DDUserModel user, JObject ParameterDictionary)
        {
            using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimSavingData.aspx")))
            {
                ERPResponseGeneral unbindResponse = ChangeUserIDResponse(null, ParameterDictionary, user, client, "Logout");
                if (unbindResponse != null && "1".Equals(unbindResponse.statusCode))
                {
                    return unbindResponse;
                }
                return null;
            }
        }

        private void ProcessChangeDDIdParameter(Dictionary<string, string> list, string ERPUserId, string ChangedDDUserId)
        {
            try
            {
                list["TableID"] = ERPUserId;
                list["rand"] = DateTime.Now.ToString("f");
                list.Add("Support14", ChangedDDUserId);
            }
            catch (Exception ex)
            {
                string parameter = string.Join(";", list.Select(x => x.Key + "=" + x.Value).ToArray());
                LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----error when process ProcessChangeDDIdParameter,the parameter is:" + parameter);
                throw;
            }
        }

        private void ProcessLoginRequestParameter(Dictionary<string, string> list, string userId)
        {
            try
            {
                list["UserID"] = userId;
                list["rand"] = DateTime.Now.ToString("f");
                list["WhereStr"] = list["WhereStr"].Replace("${DDUserId}", userId);
            }
            catch (Exception ex)
            {
                string parameter = string.Join(";", list.Select(x => x.Key + "=" + x.Value).ToArray());
                LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----error when process ProcessLoginRequestParameter, the parameter is:" + parameter);
                throw;
            }
        }

        private void processIfUserExistParameter(Dictionary<string, string> param, DDUserModel user)
        {
            try
            {
                string password = "";

                if (!string.IsNullOrEmpty(user.Password))
                {
                    password = MD5Encriptor.Encriptor(user.Password);
                }

                param["WhereStr"] = param["WhereStr"].Replace("${Username}", string.IsNullOrEmpty(user.Username) ? "" : user.Username.Trim())
                                                     .Replace("${Password}", password)
                                                     .Replace("${ERPUserId}", string.IsNullOrEmpty(user.ERPUserId) ? "" : user.ERPUserId.Trim())
                                                     .Replace("${DDUserId}", string.IsNullOrEmpty(user.DDUserId)?"":user.DDUserId.Trim());
                param["rand"] = DateTime.Now.ToString("f");
            }
            catch (Exception ex)
            {
                string parameter = string.Join(";", param.Select(x => x.Key + "=" + x.Value).ToArray());
                LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----error when process processIfUserExistParameter, the parameter is: " + parameter);
                throw;
            }

        }

        private void ProcessLogoutParameter(Dictionary<string, string> list, string ERPUserId, string oldDDUserId)
        {
            try
            {
                list["TableID"] = ERPUserId;
                list["rand"] = DateTime.Now.ToString("f");
                list.Add("Support14", "");
            }
            catch (Exception ex)
            {
                string parameter = string.Join(";", list.Select(x => x.Key + "=" + x.Value).ToArray());
                LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----error when process ProcessLogoutParameter,the parameter is:" + parameter);
                throw;
            }
        }

        private ERPResponseGeneral ChangeUserIDResponse(UserInfoViewModel model, JObject ParameterDictionary, DDUserModel user, HttpClient client, string parameterId)
        {
            try
            {
                string paramterString = ParameterDictionary[parameterId].ToString(Formatting.None);
                var paramterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(paramterString);
                switch (parameterId)
                {
                    case "ChangeDDId":
                        ProcessChangeDDIdParameter(paramterDictionary, model.ERPUserID, user.DDUserId);
                        break;
                    case "ClearDDId":
                        ProcessClearDDIDParameter(paramterDictionary, user.DDUserId);
                        break;
                    case "Logout":
                        ProcessLogoutParameter(paramterDictionary, user.ERPUserId, user.DDUserId);
                        break;
                }

                HttpContent content = new FormUrlEncodedContent(paramterDictionary);
                HttpResponseMessage response = client.PostAsync("http://erptest.bb-pco.com/KPIGetData/DimSavingData.aspx", content).Result;

                string stringResult = response.Content.ReadAsStringAsync().Result;

                JObject obj;
                ERPResponseGeneral generalRes;

                try
                {
                    obj = JObject.Parse(stringResult);
                    generalRes = new ERPResponseGeneral().serializeSaveDataResponse(obj);
                }
                catch (Exception ex)
                {
                    string parameter = string.Join(";", paramterDictionary.Select(x => x.Key + ":" + x.Value).ToArray());
                    LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----the client is returning unexpected result,the parameter is: " + parameter + "the result is: " + stringResult);
                    throw;
                }

                if ("1".Equals(generalRes.statusCode))
                {
                    return generalRes;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.ErrorInfo(this.GetType(), "Exception when ChangeUserIDResponse is running:" + parameterId, ex);
                throw;
            }
        }

        private ERPResponseGeneral ChangeUserDDId(UserInfoViewModel model, JObject ParameterDictionary, DDUserModel user,UserInfoViewModel ifExistDDIdUser)
        {
            try
            {
                using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimSavingData.aspx")))
                {
                    if (ifExistDDIdUser != null && "1".Equals(ifExistDDIdUser.returnStyle)){
                        ERPResponseGeneral clearIdResponse = ChangeUserIDResponse(model, ParameterDictionary, user, client, "ClearDDId");
                    }                 
                     ERPResponseGeneral changeIdResponse = ChangeUserIDResponse(model, ParameterDictionary, user, client, "ChangeDDId");
                     return changeIdResponse;                                        
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.ErrorInfo(this.GetType(), "Exception when ChangeUserDDId is running", ex);
                throw;
            }
        }

        private UserInfoViewModel GetUserByCondition(DDUserModel user, JObject ParameterDictionary,string parameterId)
        {
            using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimData.aspx")))
            {
                Dictionary<string, string> paramterIfExistUser = new Dictionary<string, string>();
                try
                {                  
                    string ifExistUser = ParameterDictionary[parameterId].ToString(Formatting.None);
                    paramterIfExistUser = JsonConvert.DeserializeObject<Dictionary<string, string>>(ifExistUser);
                    processIfUserExistParameter(paramterIfExistUser, user);

                    HttpContent content = new FormUrlEncodedContent(paramterIfExistUser);
                    HttpResponseMessage response = client.PostAsync("http://erptest.bb-pco.com/KPIGetData/DimData.aspx", content).Result;

                    var stringResult = response.Content.ReadAsStringAsync().Result;
                    JObject jObj;
                    ERPResponseGeneral result;

                    try
                    {
                        jObj = JObject.Parse(stringResult);
                        result = new ERPResponseGeneral().serializeRequestDataResponse(jObj);
                    }
                    catch (Exception ex)
                    {
                        string parameter = string.Join(";", paramterIfExistUser.Select(x => x.Key + ":" + x.Value).ToArray());
                        LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----the client is returning unexpected result,the parameter is: " + parameter + "the result is: " + stringResult);
                        throw;
                    }

                    UserInfoViewModel model = new UserInfoViewModel();
                    if ("1".Equals(result.statusCode))
                    {
                        model.ToUserInfoViewModel(model, jObj["data"][0]);
                        model.returnStyle = "1";
                    }
                    else
                    {
                        model.returnStyle = result.statusCode + ":" + result.errorMessage;
                        LoggerHelper.LogInfo(this.GetType(), "----get userinfo by user name and password is not returning status code 1,the status code is: " + model.returnStyle + "; the returning result is: " + stringResult);
                    }
                    return model;
                }
                catch (Exception ex)
                {
                    string parameter = string.Join(";", paramterIfExistUser.Select(x => x.Key + "=" + x.Value).ToArray());
                    LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----Exception when GetUserByUserIdAndPassword() is running, the request parameter is: " + parameter, ex);
                    throw;
                }
            }
        }
        #endregion
    }
}
