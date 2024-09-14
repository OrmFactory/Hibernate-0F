using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using PluginInterfaces;
using PluginInterfaces.Structure;

namespace HibernateGenerator;

public class ModelGenerator : IGenerator
{
	public string Name => "Java entity generator for Hibernate ORM";
	public string Description => "Hibernate entity beans and repository generator";

	[EditableProperty("Package name")]
	public string PackageName { get; set; } = "com.example";

	public void Generate(Project project, GenerationOptions options)
	{
		foreach (var schema in project.Schemas)
		{
			foreach (var table in schema.Tables)
			{
				var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.RootPath);
				var rootDir = new DirectoryInfo(path);
				if (!rootDir.Exists) rootDir.Create();
				path = rootDir.FullName;

				string compositeKeyClassName = "";
				if (table.Columns.Count(c => c.PrimaryKey) > 1)
				{
					compositeKeyClassName = table.ClassName + "Id";
					var compositeKeyFile = Path.Combine(path, compositeKeyClassName + ".java");
					var cb = GenerateCompositeKeyClass(table);
					cb.IndentChars = options.IndentChars;
					cb.NewLineChars = options.NewLineChars;
					var compositeKeyClass = cb.ToString();
					File.WriteAllText(compositeKeyFile, compositeKeyClass);
				}

				var file = Path.Combine(path, table.ClassName + ".java");
				var tb = GenerateTableEntity(table, compositeKeyClassName);
				tb.IndentChars = options.IndentChars;
				tb.NewLineChars = options.NewLineChars;
				var entity = tb.ToString();
				File.WriteAllText(file, entity);

				file = Path.Combine(path, table.ClassName + "Repository.java");
				tb = GenerateTableRepository(table, compositeKeyClassName);
				tb.IndentChars = options.IndentChars;
				tb.NewLineChars = options.NewLineChars;
				entity = tb.ToString();
				File.WriteAllText(file, entity);
			}
		}
	}

	private CodeBuilder GenerateCompositeKeyClass(Table table)
	{
		var tb = new CodeBuilder();
		tb.AppendLine($"package {PackageName}.model;");
		tb.AppendLine();
		tb.AppendLine("public class " + table.ClassName + "Id {");
		foreach (var column in table.Columns.Where(c => c.PrimaryKey))
		{
			var type = ResolveType(column.DatabaseType, column.Nullable);
			tb.Append(GenerateField(column.FieldName, type));
			tb.Append(GenerateFieldAccessors(column.FieldName, type));
		}

		tb.AppendLine("}");
		return tb;
	}

	private CodeBuilder GenerateTableRepository(Table table, string compositeKeyClassName)
	{
		var cb = new CodeBuilder();
		cb.AppendLine($"package {PackageName}.model;");
		cb.AppendLine();
		cb.AppendLine("import org.springframework.data.repository.CrudRepository;");
		cb.AppendLine();
		cb.AppendLine($"import {PackageName}.model.{table.ClassName};");
		cb.AppendLine();
		var keyColumn = table.Columns.First(c => c.PrimaryKey);
		var keyType = ResolveType(keyColumn.DatabaseType, keyColumn.Nullable);
		if (compositeKeyClassName != "") keyType = compositeKeyClassName;
		cb.AppendLine($"public interface {table.ClassName}Repository extends CrudRepository<{table.ClassName}, {keyType}> {{");
		cb.AppendLine("}");
		return cb;
	}

	private CodeBuilder GenerateTableEntity(Table table, string compositeKeyClassName)
	{
		var accessors = new CodeBuilder();
		var fields = new CodeBuilder();

		foreach (var column in table.Columns)
		{
			if (column.PrimaryKey)
			{
				fields.AppendLine("@Id");
				if (column.AutoIncrement) fields.AppendLine("@GeneratedValue(strategy=GenerationType.AUTO)");
			}

			fields.AppendLine($"@Column(name = \"{column.ColumnName}\")");
			fields.Append(GenerateJavaDocComment(column.Comment));

			var type = ResolveType(column.DatabaseType, column.Nullable);
			fields.Append(GenerateField(column.FieldName, type));
			accessors.Append(GenerateFieldAccessors(column.FieldName, type));
		}

		bool importList = false;
		foreach (var fk in table.ForeignKeys)
		{
			var type = fk.ToColumn.Table.ClassName;
			if (fk.IsReverseKey)
			{
				type = "List<" + type + ">";
				var lowerFieldName = fk.ToColumn.FieldName.ToLower().First() + fk.ToColumn.FieldName.Remove(0, 1);
				fields.AppendLine($"@OneToMany(mappedBy = \"{lowerFieldName}\", fetch = FetchType.LAZY)");
				importList = true;
			}
			else
			{
				fields.AppendLine("@ManyToOne(fetch = FetchType.LAZY)");
				fields.AppendLine($"@JoinColumn(name = \"{fk.FromColumn.ColumnName}\", insertable=false, updatable=false)");
			}
			fields.Append(GenerateField(fk.FieldName, type));
			accessors.Append(GenerateFieldAccessors(fk.FieldName, type));
		}

		var tb = new CodeBuilder();
		tb.AppendLine($"package {PackageName}.model;");
		tb.AppendLine();
		tb.AppendLine("import jakarta.persistence.*;");
		if (importList) tb.AppendLine("import java.util.List;");

		if (compositeKeyClassName != "") tb.AppendLine($"import {PackageName}.model.{compositeKeyClassName};");
		tb.AppendLine();
		tb.AppendLine("@Entity");
		tb.AppendLine($"@Table(name = \"{table.TableName}\")");
		if (compositeKeyClassName != "") tb.AppendLine($"@IdClass({compositeKeyClassName}.class)");
		tb.Append(GenerateJavaDocComment(table.Comment));
		tb.AppendLine("public class " + table.ClassName + " {");
		tb.Append(fields);
		tb.Append(accessors);
		tb.AppendLine("}");
		return tb;
	}

	private CodeBuilder GenerateField(string upperName, string type)
	{
		var lower = upperName.ToLower().First() + upperName.Remove(0, 1);
		var cb = new CodeBuilder();
		cb.AppendLine($"private {type} {lower};");
		cb.AppendLine();
		return cb;
	}

	private CodeBuilder GenerateFieldAccessors(string upperName, string type)
	{
		var lower = upperName.ToLower().First() + upperName.Remove(0, 1);
		var cb = new CodeBuilder();
		cb.AppendLine($"public void set{upperName}({type} {lower}) {{");
		cb.AppendLine($"this.{lower} = {lower};");
		cb.AppendLine("}");
		cb.AppendLine();
		cb.AppendLine($"public {type} get{upperName}() {{");
		cb.AppendLine($"return {lower};");
		cb.AppendLine("}");
		cb.AppendLine();
		return cb;
	}

	private CodeBuilder GenerateJavaDocComment(string comment)
	{
		var cb = new CodeBuilder();
		if (comment != "")
		{
			cb.AppendLine("/**");
			cb.AppendLine("* " + comment);
			cb.AppendLine("*/");
		}
		return cb;
	}

	private string ResolveType(string type, bool nullable)
	{
		if (type.StartsWith("tinyint(1)"))
			return "Boolean";

		if (type.StartsWith("tinyint") ||
			type.StartsWith("smallint") ||
			type.StartsWith("mediumint") ||
			type.StartsWith("int"))
		{
			return "Integer";
		}

		if (type.StartsWith("timestamp") ||
			type.StartsWith("datetime"))
		{
			return "java.time.LocalDateTime";
		}

		if (type.StartsWith("varchar") ||
			type.StartsWith("char") ||
			type.StartsWith("text") ||
			type.StartsWith("set") ||
			type.StartsWith("enum") ||
			type.StartsWith("geometry"))
			return "String";

		if (type.StartsWith("year"))
			return "Integer";

		if (type.StartsWith("decimal"))
			return "java.math.BigDecimal";

		if (type.StartsWith("blob"))
			return "byte[]";

		throw new NotImplementedException("Unknown type " + type);
	}
}