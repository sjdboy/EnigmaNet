using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnigmaNet.FeiShuOpenApi.Models.Ims
{
    public class InteractiveModel
    {
        public enum ElementType
        {
            Div,
            Hr,
            Action
        }

        public enum ActionLayoutType
        {
            Bisected,
            Trisection,
            Flow
        }

        public enum ActionElementType
        {
            Button,
        }

        public enum ButtonType
        {
            Default = 1,
            Danger = 2,
            Primary = 3,
        }

        public class ButtonModel
        {
            public TextModel Text { get; set; }
            public string Url { get; set; }
            public ButtonType? Type { get; set; }
        }

        public class ActionElementModel
        {
            public ActionElementType Type { get; set; }
            public ButtonModel ButtonInfo { get; set; }
        }

        public class ActionModel
        {
            public ActionLayoutType? Layout { get; set; }
            public List<ActionElementModel> Actions { get; set; }
        }

        public enum TextTagType
        {
            PlainText = 1,
            LarkMd = 2,
        }

        public class TextModel
        {
            public TextTagType Tag { get; set; }
            public string Text { get; set; }
        }

        public class ElementModel
        {
            public ElementType Type { get; set; }
            public TextModel DivInfo { get; set; }
            public ActionModel ActionInfo { get; set; }
        }

        /// <summary>
        /// 标题背景颜色
        /// </summary>
        /// <remarks>
        /// https://open.feishu.cn/document/ukTMukTMukTM/ukTNwUjL5UDM14SO1ATN
        /// </remarks>
        public enum HeaderTemplateType
        {
            Blue = 1,
            Wathet = 2,
            Turquoise = 3,
            Green = 4,
            Yellow = 5,
            Orange = 6,
            Red = 7,
            Carmine = 8,
            Violet = 9,
            Purple = 10,
            Indigo = 11,
            Grey = 12,
        }

        public bool EnableForward { get; set; }

        public string HeaderTitle { get; set; }
        public HeaderTemplateType? HeaderTemplate { get; set; }
        public List<ElementModel> Elements { get; set; }
    }
}
