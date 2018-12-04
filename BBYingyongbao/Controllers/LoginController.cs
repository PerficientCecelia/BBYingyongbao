using System;
using System.Collections.Generic;
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
        public JSendResponse<string> test() {
            return JSendResponse<string>.CreateSuccess("successful get");
        }

        [HttpGet]
        public JSendResponse<UserInfoViewModel> Get(string DDUserId)
        {
            using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimData.aspx")))
            {
                try
                {
                    var requestParameterDictionary = JsonFileReader.ReadToDictionary("Content/Json/UserLoginERPAPIQueryString.json");

                    ProcessLoginRequestParameter(requestParameterDictionary, DDUserId);
                    HttpContent content = new FormUrlEncodedContent(requestParameterDictionary);

                    HttpResponseMessage response = client.PostAsync("http://erptest.bb-pco.com/KPIGetData/DimData.aspx", content).Result;
                    var result = response.Content.ReadAsStringAsync().Result;

                    var jobj = JObject.Parse(result);
                    ERPResponseGeneral generalInfo = new ERPResponseGeneral().serializeRequestDataResponse(jobj);

                    if ("1".Equals(generalInfo.statusCode))
                    {
                        UserInfoViewModel model = new UserInfoViewModel();
                        model.ToUserInfoViewModel(model, jobj["data"][0]);
                        return JSendResponse<UserInfoViewModel>.CreateSuccess(model);
                    }
                    return JSendResponse<UserInfoViewModel>.CreateFail(generalInfo);
                }
                catch (Exception ex)
                {
                    return JSendResponse<UserInfoViewModel>.CreateError(ex.Message);
                }
            }

        }

        private void ProcessChangeDDIdParameter(Dictionary<string, string> list, string ERPUserId, string ChangedDDUserId)
        {
            list["TableID"] = ERPUserId;
            list["rand"] = DateTime.Now.ToString("f");
            list.Add("Support14", ChangedDDUserId);
        }
        private void ProcessLoginRequestParameter(Dictionary<string, string> list, string userId)
        {
            list["UserID"] = userId;
            list["rand"] = DateTime.Now.ToString("f");
            list["WhereStr"] = list["WhereStr"].Replace("${DDUserId}", userId);
        }

        private void processIfUserExistParameter(Dictionary<string, string> param, DDUserModel user)
        {
            string password = MD5Encriptor.Encriptor(user.Password);
            param["WhereStr"] = param["WhereStr"].Replace("${Username}", user.Username.Trim()).Replace("${Password}", password);
            param["rand"]= DateTime.Now.ToString("f");
        }

        private ERPResponseGeneral ChangeUserDDId(UserInfoViewModel model, JObject ParameterDictionary, DDUserModel user)
        {
            if (!model.DDUserID.Equals(user.DDUserId))
            {
                using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimSavingData.aspx")))
                {
                    string changeDDId = ParameterDictionary["ChangeDDId"].ToString(Formatting.None);
                    var paramterChangeDDId = JsonConvert.DeserializeObject<Dictionary<string, string>>(changeDDId);
                    ProcessChangeDDIdParameter(paramterChangeDDId, model.ERPUserID, user.DDUserId);
                    //if (!string.IsNullOrEmpty(DDUserId1))
                    HttpContent content = new FormUrlEncodedContent(paramterChangeDDId);
                    HttpResponseMessage saveDataResponse = client.PostAsync("http://erptest.bb-pco.com/KPIGetData/DimSavingData.aspx", content).Result;
                    var saveDataObj = JObject.Parse(saveDataResponse.Content.ReadAsStringAsync().Result);
                    var saveDataResult = new ERPResponseGeneral().serializeSaveDataResponse(saveDataObj);
                    return saveDataResult;
                }
            }
            else
            {
                return null;
            }

        }
        private UserInfoViewModel GetUserByUserIdAndPassword(DDUserModel user, JObject ParameterDictionary)
        {
            try
            {
                using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimData.aspx")))
                {
                    //查询if 存在符合条件的username password 的人 如果没有查询到，返回username password 错误，查询到，返回user id, dd user id, 则更改dd user id对应的用户信息
                    //查询user id 对应的人，更改他的dd user id /如果上一个返回的dd id 为 空，则返回信息，成功绑定dd user id, 若非空，返回信息，成功更改dd user id 
                    string ifExistUser = ParameterDictionary["IfExistUser"].ToString(Formatting.None);
                    var paramterIfExistUser = JsonConvert.DeserializeObject<Dictionary<string, string>>(ifExistUser);
                    processIfUserExistParameter(paramterIfExistUser, user);
                    HttpContent content = new FormUrlEncodedContent(paramterIfExistUser);
                    HttpResponseMessage response = client.PostAsync("http://erptest.bb-pco.com/KPIGetData/DimData.aspx", content).Result;
                    var jObj = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    var result = new ERPResponseGeneral().serializeRequestDataResponse(jObj);
                    if ("1".Equals(result.statusCode))
                    {
                        UserInfoViewModel model = new UserInfoViewModel();
                        model.ToUserInfoViewModel(model, jObj["data"][0]);
                        return model;
                    }

                    return null;
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        [HttpPost]
        public JSendResponse<UserInfoViewModel> Post([FromBody] DDUserModel user)
        {
            try
            {
                var ParameterDictionary = JsonFileReader.ReadTOJson("Content/Json/UserLoginERPAPIPostString.json");
                UserInfoViewModel userModel = GetUserByUserIdAndPassword(user, ParameterDictionary);
                if (userModel != null)
                {
                    ERPResponseGeneral info = ChangeUserDDId(userModel, ParameterDictionary, user);
                    if ("1".Equals(info.statusCode))
                    {
                        return JSendResponse<UserInfoViewModel>.CreateSuccess(userModel);
                    }
                    else
                    {
                        return JSendResponse<UserInfoViewModel>.CreateFail(info);
                    }
                }
                else
                {
                    return JSendResponse<UserInfoViewModel>.CreateError("用户名或密码找不到哦！");
                }
            }
            catch (Exception)
            {
                return JSendResponse<UserInfoViewModel>.CreateError("exception during the request");
            }
        }

    }
}
