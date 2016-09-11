﻿using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using Horton.MigrationGenerator.Sys;

namespace Horton.MigrationGenerator.DDL
{
    public class CreateTable : AbstractDatabaseChange
    {
        public CreateTable(string objectIdentifier, IEnumerable<ColumnInfo> columns, string note)
        {
            ObjectIdentifier = objectIdentifier;
            Columns = columns;
            Note = note ?? "";
        }

        public string ObjectIdentifier { get; }
        public IEnumerable<ColumnInfo> Columns { get; }

        public List<ITableConstraintInfo> Constraints { get; } = new List<ITableConstraintInfo>();

        public string Note { get; }

        public override void AppendDDL(IndentedTextWriter textWriter)
        {
            if (Note.Length > 0)
            {
                textWriter.WriteLine("/*");
                textWriter.Write("  ");
                textWriter.WriteLine(Note);
                textWriter.WriteLine("*/");
            }

            textWriter.WriteLine($"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{ObjectIdentifier}'))");

            textWriter.Indent++;
            textWriter.WriteLine($"CREATE TABLE {ObjectIdentifier} (");

            textWriter.Indent++;
            foreach (var column in Columns)
            {
                column.AppendDDL(textWriter, includeDefaultConstraints: true);
                textWriter.WriteLine(",");
            }
            foreach (var constraint in Constraints)
            {
                constraint.AppendDDL(textWriter);
                textWriter.WriteLine(",");
            }
            textWriter.Indent--;

            textWriter.WriteLine(");");
            textWriter.Indent--;

            textWriter.WriteLine("GO");
        }

        internal static CreateTable FromSQL(Table table)
        {
            var createTable = new CreateTable(SqlUtil.GetQuotedObjectIdentifierString(table.name, table.Schema.name), table.Columns.Select(c => ColumnInfo.FromSQL(c)), "");

            createTable.Constraints.AddRange(table.Indexes.Where(x => x.is_primary_key).Select(index => new PrimaryKeyInfo
            {
                PrimaryKeyName = index.name,
                IsNonClustered = index.type_desc == "NONCLUSTERED",
                Columns = index.Columns.OrderBy(x => x.key_ordinal).Select(x => "[" + x.Column.Name + "]" + (x.is_descending_key ? " DESC" : "")),
            }));

            createTable.Constraints.AddRange(table.Indexes.Where(x => x.is_unique_constraint).Select(index => new UniqueConstraintInfo
            {
                ConstraintName = index.name,
                IsSystemNamed = index.is_system_named,
                IsNonClustered = index.type_desc == "NONCLUSTERED",
                Columns = index.Columns.OrderBy(x => x.key_ordinal).Select(x => "[" + x.Column.Name + "]" + (x.is_descending_key ? " DESC" : "")),
            }));

            createTable.Constraints.AddRange(table.ForeignKeys.Select(x => new ForeignKeyInfo
            {
                ForeignKeyObjectIdentifier = x.ForeignKeyName,
                ParentObjectIdentifier = SqlUtil.GetQuotedObjectIdentifierString(x.Parent.name, x.Parent.Schema.name),
                ParentObjectColumnName = x.ParentColumnName,
                ReferencedObjectIdentifier = SqlUtil.GetQuotedObjectIdentifierString(x.Referenced.name, x.Referenced.Schema.name),
                ReferencedObjectColumnName = x.ReferencedColumnName,
            }));
            return createTable;
        }
    }
}