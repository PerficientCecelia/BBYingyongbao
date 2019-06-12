using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using BBYingyongbao.Common;
using BBYingyongbao.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BBYingyongbao.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [Route("test")]
        [HttpGet]
        public string test()
        {
             string jsCode = "021nmcA91CpjbP1snWx91ZH3A91nmcAv";
             var info = GetWeixinAuthMessage(jsCode);
            return info.ToString();
            //var sessionKey = "ewie6bQXi7nEyBv0NEjNqQ==";

            //return HttpContext.Session.GetString("oQFXr4hJDdEKvHXCvx7Jq2P3SvbY");

            //string openId = "oQFXr4hJDdEKvHXCvx7Jq2P3SvbY";
            //string encryptedDataPlain = "cQvCLobn7yYtxKzBAPHU+a5tJyiAW960DrM4X2TjN63xTc3v89cH5818egOOLYmC6MwVOqUY5hkucEh3TyNBVVN1hBw2+lg/yL5Me+O57I7B2C8RCc9mXp3Vkag0jIHXIdmA3j3i23uTDagn66TJtC9+/ERuPAG4r/JrxVdF0BnNMOxE7qtd7VK9bY6O3feoayQHr8s4nqXYFP53Ql+JIM48M0aZkhD4iwZrkyQVpxMO+mi63FZZ6lQd+7mcE/i/8fewvcEUaFEioESaoRsDlGyYsy+8IyAo7OHBEMYD8sMw9niEjZhLHe9Y6PoGyCI8S6oEGEbRQIQ5/IJyo969T7fWe++dVXYj6y7Lu9vBxt/TbmSj8rQinObJLmeY2LsNCFI4U/7Srb0LSVg3ZGHHxhbPYUdwaq5ku22qHyTcePjdZTbFbCt7/EWv5ptxT/oY";
            //string iv = "u+IPke+FkEYopqBF+4kimQ==";
            //var info = DecodeInfo(new DecodeInfoModel() { encryptedDataPlain = encryptedDataPlain, iv = iv, openId = openId });
            //return JsonConvert.SerializeObject(info);
           // var erp = config.Value.DimDataLink;
            //return config.Value.ToString();
        }

        [Route("test1")]
        [HttpGet]
        public string test1()
        {
            HttpContext.Session.SetString("oQFXr4hJDdEKvHXCvx7Jq2P3SvbY", "nMdIEY2lniYi0/WQZZ0Tdw==");
            return "test success";
        }
        private readonly IOptions<ERPConfig> config;

        public LoginController(IOptions<ERPConfig> config)
        {
            this.config = config;
            DimDataLink = config.Value.DimDataLink;
            DimSavingDataLink = config.Value.DimSavingDataLink;
            weixinSecret = config.Value.weixinSecret;
            weixinAppID = config.Value.weixinAppID;
            appkey = config.Value.ddappkey;
            appSecret = config.Value.ddappSecret;
        }

        public string DimDataLink;
        public string DimSavingDataLink;
        public static string Code2SessionUrl = "https://api.weixin.qq.com/sns/jscode2session?appid={0}&secret={1}&js_code={2}&grant_type=authorization_code";
        public static string Code2SessionBaseUrl = "https://api.weixin.qq.com/sns/jscode2session";
        public string weixinSecret;
        public string weixinAppID;
        public string appkey;
        public string appSecret;

        [HttpGet]
        [Route("GetWeixinAuthMessage")]
        public JSendResponse<WeiXinUserInfo> GetWeixinAuthMessage(string jsCode)
        {
            var getCode2SessionUrl = string.Format(Code2SessionUrl, weixinAppID, weixinSecret, jsCode);
            var openid = "";
            var session_key = "";
            var unionid = "";
            StringBuilder errorMessage = new StringBuilder();
            using (var client = HttpClientHelper.GetClient(new Uri(Code2SessionBaseUrl)))
            {
                HttpResponseMessage response = client.GetAsync(getCode2SessionUrl).Result;
                string resultString = response.Content.ReadAsStringAsync().Result;
                JObject resultObj = JObject.Parse(resultString);
                var status = resultObj["errcode"];
                if (status != null)
                {
                    errorMessage.Append(resultString);
                    WeiXinUserInfo info = new WeiXinUserInfo().Convert("", "", "", errorMessage.ToString());
                    return JSendResponse<WeiXinUserInfo>.CreateFail(info);
                }
                else
                {
                    session_key = resultObj["session_key"].ToString();
                    openid = resultObj["openid"].ToString();
                    unionid = resultObj["unionid"] == null ? "" : resultObj["unionid"].ToString();
                    HttpContext.Session.SetString(openid, session_key);
                    WeiXinUserInfo info = new WeiXinUserInfo().Convert(unionid, openid, session_key, errorMessage.ToString());
                    return JSendResponse<WeiXinUserInfo>.CreateSuccess(info);
                }
            }
        }

        [HttpPost]
        [Route("DecodeInfo")]
        public object DecodeInfo(DecodeInfoModel info)
        {
            string session_key;
            try
            {
                session_key = HttpContext.Session.GetString(info.openId);
                if (string.IsNullOrEmpty(session_key))
                {
                    throw new Exception("Session Value can not be null");
                }
            }
            catch (Exception ex)
            {
                string message = "error message: " + ex.Message + "exception happened when you are getting the session key, encryptedDataPlain: " + info.encryptedDataPlain + " iv:" + info.iv + " openId:" + info.openId;
                LoggerHelper.ErrorInfo(this.GetType(), message);
                return message;
            }
            try
            {
                string result = WeixinAESEncryptor.AESDecriptor(info.encryptedDataPlain, info.iv, session_key);
                return result;
            }
            catch (Exception ex)
            {
                string message = "error message: " + ex.Message + "encryptedDataPlain: " + info.encryptedDataPlain + " iv:" + info.iv + " openId:" + info.openId;
                LoggerHelper.ErrorInfo(this.GetType(), message);
                return ex;
            }
        }

        [HttpGet]
        [Route("ddid")]
        public JSendResponse<object> GetDDUserId(string authCode)
        {
            var Access_tokenUrl = "https://oapi.dingtalk.com/gettoken?appkey=" + appkey + "&appsecret=" + appSecret;
            using (HttpClient client = HttpClientHelper.GetClient(new Uri("https://oapi.dingtalk.com")))
            {
                HttpResponseMessage response = client.GetAsync(Access_tokenUrl).Result;
                JObject resultObj = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                var status = resultObj["errcode"].ToString();
                if ("0".Equals(status))
                {
                    var accesstoken = resultObj["access_token"].ToString();
                    var DDUserIdUrl = "https://oapi.dingtalk.com/user/getuserinfo?access_token=" + accesstoken + "&code=" + authCode;
                    var DDIdresponse = client.GetAsync(DDUserIdUrl).Result;
                    var stringContent = DDIdresponse.Content.ReadAsStringAsync().Result;
                    var objContent = JsonConvert.DeserializeObject(stringContent);
                    return JSendResponse<object>.CreateSuccess(objContent);
                }
                return JSendResponse<object>.CreateFail("can not get Access token from DD");
            }

        }

        [HttpPost]
        [Route("logout")]
        public JSendResponse<ERPResponseGeneral> Logout(string id)
        {
            DDUserModel user = JsonConvert.DeserializeObject<DDUserModel>(id);

            LoggerHelper.LogInfo(this.GetType(), user.ToDictionaryString(user));

            if (!ValidateLogoutRequestParameter(user))
            {
                LoggerHelper.LogInfo(this.GetType(), "----the request parameter is not passing proper parameter: " + user.ToDictionaryString(user));
                return JSendResponse<ERPResponseGeneral>.CreateFail(user);
            }

            var ParameterDictionary = JsonFileReader.ReadTOJson("Content/Json/UserLogoutAPIPostString.json");
            UserInfoViewModel userModel = GetUserByCondition(user, ParameterDictionary, "IfExistUser").FirstOrDefault();
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
                if (!ValidateLoginByUsernameAndPassword(user))
                {
                    LoggerHelper.LogInfo(this.GetType(), "----the request parameter is not passing proper parameter: " + user.ToDictionaryString(user));
                    return JSendResponse<UserInfoViewModel>.CreateFail(user);
                }

                var ParameterDictionary = JsonFileReader.ReadTOJson("Content/Json/UserLoginERPAPIPostString.json");
                List<UserInfoViewModel> userModelList = GetUserByCondition(user, ParameterDictionary, "IfExistUser");


                UserInfoViewModel userModel = IfUserExistWithSpecificPassword(userModelList, user.Password);

                if (userModel != null)
                {
                    UserInfoViewModel ifExistDDIdUser = GetUserByCondition(user, ParameterDictionary, "ifExistDDIdUser").FirstOrDefault();
                    ERPResponseGeneral info = ChangeUserDDId(userModel, ParameterDictionary, user, ifExistDDIdUser);
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

                LoggerHelper.LogInfo(this.GetType(), "GetUserByUserIdAndPassword is not returning correct status code,please recheck the password,the user info:" + user.Username);
                return JSendResponse<UserInfoViewModel>.CreateFail(userModel);
            }
            catch (Exception ex)
            {
                LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "----Exception when LoginController post() is running", ex);
                return JSendResponse<UserInfoViewModel>.CreateError(ex.Message);
            }
        }

        private UserInfoViewModel IfUserExistWithSpecificPassword(List<UserInfoViewModel> userModelList, string password)
        {
            string encriptedPassword = "";

            if (!string.IsNullOrEmpty(password))
            {
                encriptedPassword = MD5Encriptor.Encriptor(password);
            }

            return userModelList.FirstOrDefault(u => u.Password.Trim().Equals(encriptedPassword));
        }

        [HttpGet]
        public JSendResponse<UserInfoViewModel> Get(string DDUserId)
        {
            if (string.IsNullOrEmpty(DDUserId))
            {
                LoggerHelper.LogInfo(this.GetType(), "----the request parameter is not passing proper parameter: DDUserId:" + DDUserId);
                return JSendResponse<UserInfoViewModel>.CreateFail("DDUserId:" + DDUserId);
            }
            using (HttpClient client = HttpClientHelper.GetClient(new Uri(DimDataLink)))
            {
                try
                {
                    var requestParameterDictionary = JsonFileReader.ReadToDictionary("Content/Json/UserLoginERPAPIQueryString.json");

                    ProcessLoginRequestParameter(requestParameterDictionary, DDUserId);
                    HttpContent content = new FormUrlEncodedContent(requestParameterDictionary);

                    HttpResponseMessage response = client.PostAsync(DimDataLink, content).Result;
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
        private bool ValidateLogoutRequestParameter(DDUserModel user)
        {
            if (string.IsNullOrEmpty(user.DDUserId) || string.IsNullOrEmpty(user.ERPUserId))
            {
                return false;
            }
            return true;
        }

        private bool ValidateGetUserByDDUserID(DDUserModel user)
        {
            if (string.IsNullOrEmpty(user.DDUserId))
            {
                return false;
            }
            return true;
        }

        private bool ValidateLoginByUsernameAndPassword(DDUserModel user)
        {
            if (string.IsNullOrEmpty(user.DDUserId) || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                return false;
            }
            return true;
        }
        private void ProcessClearDDIDParameter(Dictionary<string, string> list, string ChangedDDUserId)
        {
            try
            {
                list["TableID"] = ChangedDDUserId;
                list["rand"] = DateTime.Now.ToString("u");
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
            using (HttpClient client = HttpClientHelper.GetClient(new Uri(DimSavingDataLink)))
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
                list["rand"] = DateTime.Now.ToString("u");
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
                list["rand"] = DateTime.Now.ToString("u");
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

                param["WhereStr"] = param["WhereStr"].Replace("${Username}", string.IsNullOrEmpty(user.Username) ? "" : user.Username.Trim())
                                                     .Replace("${ERPUserId}", string.IsNullOrEmpty(user.ERPUserId) ? "" : user.ERPUserId.Trim())
                                                     .Replace("${DDUserId}", string.IsNullOrEmpty(user.DDUserId) ? "" : user.DDUserId.Trim());
                param["rand"] = DateTime.Now.ToString("u");
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
                list["rand"] = DateTime.Now.ToString("u");
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
                HttpResponseMessage response = client.PostAsync(DimSavingDataLink, content).Result;

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

        private ERPResponseGeneral ChangeUserDDId(UserInfoViewModel model, JObject ParameterDictionary, DDUserModel user, UserInfoViewModel ifExistDDIdUser)
        {
            try
            {
                using (HttpClient client = HttpClientHelper.GetClient(new Uri(DimSavingDataLink)))
                {
                    if (ifExistDDIdUser != null && "1".Equals(ifExistDDIdUser.returnStyle))
                    {
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

        private List<UserInfoViewModel> GetUserByCondition(DDUserModel user, JObject ParameterDictionary, string parameterId)
        {
            using (HttpClient client = HttpClientHelper.GetClient(new Uri(DimDataLink)))
            {
                Dictionary<string, string> paramterIfExistUser = new Dictionary<string, string>();
                try
                {
                    string ifExistUser = ParameterDictionary[parameterId].ToString(Formatting.None);
                    paramterIfExistUser = JsonConvert.DeserializeObject<Dictionary<string, string>>(ifExistUser);
                    processIfUserExistParameter(paramterIfExistUser, user);

                    HttpContent content = new FormUrlEncodedContent(paramterIfExistUser);
                    HttpResponseMessage response = client.PostAsync(DimDataLink, content).Result;
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

                    List<UserInfoViewModel> list = new List<UserInfoViewModel>();
                    if ("1".Equals(result.statusCode))
                    {
                        var resultData = (JArray)jObj["data"];
                        foreach (JToken data in resultData)
                        {
                            UserInfoViewModel model = new UserInfoViewModel();
                            model.ToUserInfoViewModel(model, data);
                            model.returnStyle = "1";
                            list.Add(model);
                        }
                    }
                    else
                    {
                        LoggerHelper.LogInfo(this.GetType(), "----get userinfo by user name and password is not returning status code 1; the returning result is: " + stringResult);
                    }
                    return list;
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
