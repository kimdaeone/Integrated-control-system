// JSON 파일에서 읽은 WidgetType을 스캔하여 알맞은 클래스로 변환하는 공간.
// 새로 추가할 때 switch 문에 case를 추가한다.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IPROPHETA.Core;
using IPROPHETA.Core.Models;
using IPROPHETA.UI.Widgets;


namespace IPROPHETA.Utils
{
    /// <summary>
    /// 추상 클래스인 WidgetBase를 실제 위젯 클래스(Button, Sensor 등)로 
    /// 올바르게 변환해주는 커스텀 JSON 컨버터입니다.
    /// </summary>
    public class WidgetConverter : JsonConverter
    {
        // 이 컨버터가 WidgetBase 타입의 객체를 처리할 수 있는지 확인
        public override bool CanConvert(Type objectType)
        {
            return typeof(WidgetBase).IsAssignableFrom(objectType);
        }

        // JSON을 읽어서 실제 객체로 만드는 핵심 로직
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // A. 데이터를 JObject(JSON 객체 모델)로 일단 메모리에 로드합니다.
            JObject jo = JObject.Load(reader);
            
            // B. [핵심] JSON 안에 적힌 "Type" 값을 읽어옵니다. 
            // 이 값은 우리가 Phase 1.1 Enums.cs에서 정의한 '위젯의 주민번호'와 같습니다.
            WidgetType type = jo["Type"].ToObject<WidgetType>();
            
            // C. 판별된 타입에 맞는 '실제 그릇'을 준비합니다.
            WidgetBase widget;
            switch (type)
            {
                // widget = new ButtonWidget(); // 나중에 만들 실제 클래스
                case WidgetType.Button:
                    widget = new ButtonWidget(); // 이제 에러 대신 진짜 객체를 생성합니다!
                    break;
                
                default:
                    throw new Exception($"미구현 타입: {type}");
            }
            // D. [중요] 비어있는 그릇(widget)에 나머지 데이터(이름, 위치, 색상 등)를 들이붓습니다.
            // Populate는 기존 객체의 속성들을 JSON 값으로 덮어씌워주는 아주 편리한 기능입니다.
            serializer.Populate(jo.CreateReader(), widget);
            return widget;
        }

        // 저장(Write)은 기본 로직을 사용하므로 구현하지 않음. 따라서 false
        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();
    }
}