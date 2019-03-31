using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BBYingyongbao.Common;
using BBYingyongbao.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BBYingyongbao.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderCalendarController : ControllerBase
    {
        [Route("test")]
        [HttpGet]
        public string test() {
            return "test success";
        }

        [Route("orders")]
        [HttpGet]
        public JSendResponse<calendarObj> GetOrderCalender(string ERPUserId, string TimeFrom, string TimeTo)
        {

            OrdersParameters para = ValidateRequestParameter(ERPUserId, TimeFrom, TimeTo);
            if (!para.IsPassed)
            {
                LoggerHelper.LogInfo(this.GetType(), "----the request parameter is not passing proper parameter: ERPUserId:" + ERPUserId);
                return JSendResponse<calendarObj>.CreateFail("ERPUserId:" + ERPUserId);
            }

            using (HttpClient client = HttpClientHelper.GetClient(new Uri("http://erptest.bb-pco.com/KPIGetData/DimData.aspx")))
            {
                try
                {
                    var paraDic = JsonFileReader.ReadTOJson("Content/Json/OrderCalendarERPAPIQueryString.json");
                    var requestParameterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(paraDic["GetOrders"].ToString());
                    var headersRequestParameterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(paraDic["GetMainTableHeaders"].ToString());

                    HttpContent headercontent = new FormUrlEncodedContent(headersRequestParameterDictionary);
                    HttpResponseMessage headersResponse = client.PostAsync("http://erptest.bb-pco.com/KPIGetData/DimData.aspx", headercontent).Result;
                    var headerstring = headersResponse.Content.ReadAsStringAsync().Result;
                    JObject obj = JObject.Parse(headerstring);
                    JArray array = (JArray)obj["data"];

                    var a = array.OrderBy(t => t["ShowOrder"]).Select(t => t["SQLColumnName"].ToString());
                    var searchTargetColumn = string.Join(",", a.ToArray());

                    ProcessOrdersRequestParameter(requestParameterDictionary, para.ERPUserId, para.TimeFrom, para.TimeTo, searchTargetColumn);
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
                        var list = (JArray)jobj["data"];
                        List<OrderDetailViewModel> orders = list.Select(o => new OrderDetailViewModel()
                        {
                            ContractNumber = o["Name6"].ToString(),
                            OrderNumber = o["flowfullordernum"].ToString(),
                            ProjectNumber = o["Name5"].ToString(),
                            ClientName = o["Chose3KeyValue"].ToString(),
                            ProjectName = o["Name1"].ToString(),
                            PlanedStartTime = string.IsNullOrEmpty(o["UsertimeEnd13"].ToString()) ? DateTime.MinValue : DateTime.Parse(o["UsertimeEnd13"].ToString()),
                            PlanedEndTime = string.IsNullOrEmpty(o["UserTimeFrom14"].ToString()) ? DateTime.MinValue : DateTime.Parse(o["UserTimeFrom14"].ToString()),
                            RealStartTime = string.IsNullOrEmpty(o["usertimefrom20"].ToString()) ? DateTime.MinValue : DateTime.Parse(o["usertimefrom20"].ToString()),
                            RealEndTime = string.IsNullOrEmpty(o["usertimeend20"].ToString()) ? DateTime.MinValue : DateTime.Parse(o["usertimeend20"].ToString()),
                            WorkerId = o["chose2key"].ToString(),
                            WorkerName = o["Chose2KeyValue"].ToString(),
                            ServiceReport = o["script7"].ToString(),
                            Rators = o["Support20"].ToString(),
                            Passed = o["passed"].ToString().Equals("0") ? false : true,
                            DepartmentId = o["Chose4Key"].ToString(),
                            DepartmentName = o["Chose4KeyValue"].ToString(),
                            CompanyId = o["Chose5Key"].ToString(),
                            CompanyName = o["Chose5KeyValue"].ToString(),
                            GenerateTime = string.IsNullOrEmpty(o["GenerateTime"].ToString()) ? DateTime.MinValue : DateTime.Parse(o["GenerateTime"].ToString())
                        }).ToList();
                        var OrderCalendar = orders.GroupBy(o => o.PlanedStartTimeDay).Select(x => new OrderCalendarViewModel() { Day = x.Key, TodoCount = x.Count(), list = x.ToList() }).ToList();
                        var headers = JsonConvert.DeserializeObject(headerstring);

                        var calendarObj = new calendarObj() {
                            headers=headers,
                            orderCalendar=OrderCalendar
                        };
                        return JSendResponse<calendarObj>.CreateSuccess(calendarObj);
                    }

                    LoggerHelper.LogInfo(this.GetType(), "----get usermodel by DDUserId is not returning status code 1,the status code is: " + generalInfo.statusCode);
                    return JSendResponse<calendarObj>.CreateFail(generalInfo);
                }
                catch (Exception ex)
                {
                    LoggerHelper.ErrorInfo(ex.GetType(), ex.Message + "---- Exception when Login Controller get is running");
                    return JSendResponse<calendarObj>.CreateError(ex.Message);
                }
            }
        }

        private OrdersParameters ValidateRequestParameter(string ERPUserId, string TimeFrom, string TimeTo)
        {
            OrdersParameters parameters = new OrdersParameters();
            if (string.IsNullOrEmpty(TimeFrom))
            {
                TimeFrom = DatetimeUtil.ConvertDateTime(DateTime.Now.AddMonths(-1));
            }
            parameters.TimeFrom = TimeFrom;
            if (string.IsNullOrEmpty(TimeFrom))
            {
                TimeFrom = DatetimeUtil.ConvertDateTime(DateTime.Now.AddMonths(-1));
            }
            parameters.TimeTo = TimeTo;
            parameters.ERPUserId = ERPUserId;
            parameters.IsPassed = true;

            if (string.IsNullOrEmpty(ERPUserId))
            {
                parameters.IsPassed = false;
            }
            return parameters;
        }

        private void ProcessOrdersRequestParameter(Dictionary<string, string> list, string ERPUserId, string TimeFrom, string TimeTo, string TargetColumn)
        {
            list["rand"] = DateTime.Now.ToString("u");
            list["WhereStr"] = list["WhereStr"].Replace("${ERPUserId}", ERPUserId).Replace("${TimeFrom}", TimeFrom).Replace("${TimeTo}", TimeTo);
            list["TargetColumn"] = TargetColumn;
        }
    }

    public class OrdersParameters
    {
        public string ERPUserId { get; set; }

        public string TimeFrom { get; set; }

        public string TimeTo { get; set; }

        public bool IsPassed { get; set; }
    }

    public class calendarObj {
        public dynamic headers { get; set; }

        public List<OrderCalendarViewModel> orderCalendar { get; set; }
    }
}