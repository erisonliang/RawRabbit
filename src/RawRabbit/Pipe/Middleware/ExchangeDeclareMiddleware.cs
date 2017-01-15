﻿using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class ExchangeDeclareOptions
	{
		public Func<IPipeContext, ExchangeDeclaration> ExchangeFunc { get; set; }
		public bool ThrowOnFail { get; set; }
		public  Func<IPipeContext, bool> ThrowOnFailFunc { get; set; }
	}

	public class ExchangeDeclareMiddleware : Middleware
	{
		protected readonly ITopologyProvider TopologyProvider;
		protected readonly Func<IPipeContext, ExchangeDeclaration> ExchangeFunc;
		protected Func<IPipeContext, bool> ThrowOnFailFunc;
		private readonly ILogger _logger = LogManager.GetLogger<ExchangeDeclareMiddleware>();

		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider, ExchangeDeclareOptions options = null)
		{
			TopologyProvider = topologyProvider;
			ExchangeFunc = options?.ExchangeFunc;
			ThrowOnFailFunc = options?.ThrowOnFailFunc ?? (context => false);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var exchangeCfg = GetExchangeDeclaration(context);

			if (exchangeCfg != null)
			{
				_logger.LogDebug($"Exchange configuration found. Declaring '{exchangeCfg.Name}'.");
				return DeclareExchangeAsync(exchangeCfg, context, token)
					.ContinueWith(t => Next.InvokeAsync(context, token), token)
					.Unwrap();
			}

			if (GetThrowOnFail(context))
			{
				throw new ArgumentNullException(nameof(exchangeCfg));
			}
			return Next.InvokeAsync(context, token);
		}

		protected virtual ExchangeDeclaration GetExchangeDeclaration(IPipeContext context)
		{
			return ExchangeFunc?.Invoke(context);
		}

		protected virtual bool GetThrowOnFail(IPipeContext context)
		{
			return ThrowOnFailFunc(context);
		}

		protected virtual Task DeclareExchangeAsync(ExchangeDeclaration exchange, IPipeContext context, CancellationToken token)
		{
			return TopologyProvider.DeclareExchangeAsync(exchange);
		}
	}
}