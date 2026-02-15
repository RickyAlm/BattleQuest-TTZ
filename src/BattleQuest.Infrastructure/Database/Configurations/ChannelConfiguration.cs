using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
	{
		public void Configure(EntityTypeBuilder<Channel> builder)
		{
			builder.ToTable("Channel");

			builder.HasKey(c => c.ChannelId);

			builder.Property(c => c.Name)
				.IsRequired()
				.HasColumnType("varchar(30)");

			builder.HasIndex(c => c.Name)
				.IsUnique();
		}
	}
}
