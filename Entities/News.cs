using System;
using System.Collections.Generic;

namespace IFXClient.Entities
{
    /// <summary>
    /// Новость
    /// </summary>
    public class News
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Заголовок
        /// </summary>
        public string headline { get; set; }

        /// <summary>
        /// Дата публикации (MSK)
        /// </summary>
        public DateTime publication_time { get; set; }

        /// <summary>
        /// Тело
        /// </summary>
        public string body { get; set; }

        /// <summary>
        /// Список идентификаторов продуктов
        /// </summary>
        public List<string> product_ids { get; set; }
    }
}
