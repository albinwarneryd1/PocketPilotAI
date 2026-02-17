namespace PocketPilotAI.Core.Domain.Entities;

public class Category
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid UserId { get; set; }

  public string Name { get; set; } = string.Empty;

  public string ColorHex { get; set; } = "#2F855A";

  public bool IsSystem { get; set; }

  public Guid? ParentCategoryId { get; set; }

  public User? User { get; set; }

  public Category? ParentCategory { get; set; }

  public ICollection<Category> SubCategories { get; set; } = new List<Category>();

  public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

  public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
