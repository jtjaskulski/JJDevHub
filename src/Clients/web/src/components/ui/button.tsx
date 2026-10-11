import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from 'cn';
import { Slot } from 'radix-ui';
import type { ComponentProps } from 'react';

const buttonVariants = cva(
  'inline-flex shrink-0 items-center justify-center rounded-md border border-transparent text-small font-medium whitespace-nowrap transition-colors outline-none select-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:bg-primary/80',
        outline: 'border-ink/25 bg-card text-foreground hover:bg-ink/8',
        secondary: 'bg-secondary text-secondary-foreground hover:bg-ink/8',
        ghost: 'hover:bg-ink/8',
        destructive: 'bg-ink text-white hover:bg-ink/80',
        link: 'text-primary underline-offset-4 hover:underline',
      },
      size: {
        default: 'gap-2 px-4 py-2',
        xs: 'gap-1 px-2 py-1 text-caption',
        sm: 'gap-2 px-3 py-2',
        lg: 'gap-2 px-5 py-3',
        icon: 'p-2',
        'icon-xs': 'p-1',
        'icon-sm': 'p-2',
        'icon-lg': 'p-3',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
);

function Button({
  className,
  variant = 'default',
  size = 'default',
  asChild = false,
  ...props
}: ComponentProps<'button'> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
  }) {
  const Comp = asChild ? Slot.Root : 'button';

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  );
}

export { Button, buttonVariants };
