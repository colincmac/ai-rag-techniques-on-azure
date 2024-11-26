
from typing import List, TypedDict
import pandas as pd
import numpy as np
import plotly.express as px
from scipy.optimize import curve_fit

class ScenarioAssumptions:
    subscriber_growth: List[float]
    renewal_rate: List[float]
    arpu_growth: List[float]
    marketing_expense_ratio: List[float]
    cost_of_revenue_ratio: List[float]

    def _exp_growth_func(self, x, a, b):
        return a * np.exp(b * x)

    def _logistic_growth_func(self, time: np.ndarray[int], K: int, P0: int, r: float):
        """
        K = TAM: the carrying capacity of the environment

        P0 = the initial population/users

        r = the growth rate absolute value

        1((K - P0)/P0)1 = the maximum growth rate

        `np.exp(-r * x)` represents the exponential decay of the growth rate over time.
        """
        return K / (1 + ((K - P0)/P0) * np.exp(-r * time))

    # Apply Exponential Model: Assumes growth is proportional to the current size of the market. Does not account for market saturation.
    def forecast_exp_growth(self, years: np.ndarray[int], metric: np.ndarray[int], forecast_years: np.ndarray[int]):
        """
        years: array of historical years for user data
        """
        params, pcov = curve_fit(self._exp_growth_func, years - years[0], metric)
        forecast = self._exp_growth_func(forecast_years - years[0], *params)
        return forecast

    # Estimate Total Addressable Market (TAM): Research industry reports, demographics, and market data.
    # Apply Logistic Model: Use the logistic function to model growth slowing over time. Accounts for slowing growth as market saturation approaches.
    def forecast_logistic_growth(self, years: np.ndarray[int], users: np.ndarray[int], forecast_years: np.ndarray[int], current_growth_rate: float, est_tam: int):
        #  that the carrying capacity (K) does not exceed the estimated TAM (est_tam), and the initial population (P0) does not exceed the initial number of subscribers
        bounds = (0, [est_tam, users[0], np.inf])

        params, pcov = curve_fit(self._logistic_growth_func, years - years[0], users, p0=[est_tam, users[0], current_growth_rate], bounds=bounds)
        forecast = self._logistic_growth_func(forecast_years - years[0], *params)
        return forecast

    def run(years: List[int], starting_subscribers: int, starting_arpu: int, discount_rate: float, current_share_price: int, shares_outstanding: int):
        financials = {}
        # subscriber growth, renewal rate, ARPU growth, marketing expense ratio, and cost of revenue ratio should be calculated based on historical data, industry benchmarks, and management guidance.
        assumptions = {
            'Base Case': {
                'subscriber_growth': [0.10, 0.09, 0.08, 0.07, 0.06], 
                'renewal_rate': [0.85] * 5,
                'arpu_growth': [0.05] * 5,
                'marketing_expense_ratio': [0.15, 0.14, 0.13, 0.12, 0.11],
                'cost_of_revenue_ratio': [0.50, 0.48, 0.46, 0.44, 0.42],
            },
            'Upside Case': {
                'subscriber_growth': [0.15, 0.14, 0.13, 0.12, 0.11],
                'renewal_rate': [0.88] * 5,
                'arpu_growth': [0.06] * 5,
                'marketing_expense_ratio': [0.14, 0.13, 0.12, 0.11, 0.10],
                'cost_of_revenue_ratio': [0.48, 0.46, 0.44, 0.42, 0.40],
            },
            'Downside Case': {
                'subscriber_growth': [0.05, 0.04, 0.03, 0.02, 0.01],
                'renewal_rate': [0.80] * 5,
                'arpu_growth': [0.04] * 5,
                'marketing_expense_ratio': [0.16, 0.15, 0.14, 0.13, 0.12],
                'cost_of_revenue_ratio': [0.52, 0.50, 0.48, 0.46, 0.44],
            },
            'Extreme Downside Case': {
                'subscriber_growth': [0.00, -0.01, -0.02, -0.03, -0.04],
                'renewal_rate': [0.75] * 5,
                'arpu_growth': [0.02] * 5,
                'marketing_expense_ratio': [0.18, 0.17, 0.16, 0.15, 0.14],
                'cost_of_revenue_ratio': [0.55, 0.53, 0.51, 0.49, 0.47],
            },
        }
        for scenario in assumptions.keys():
            df = pd.DataFrame(index=years)
            financials[scenario] = df


        # starting_subscribers = 1000000  # Example starting point
        # starting_arpu = 10  # Example ARPU
        # discount_rate = 0.10  # Example discount rate
        # current_share_price = 550  # Example current share price
        # shares_outstanding = 1000000  # Example number of shares
        summary = []

        for scenario, df in financials.items():
            # Project Revenue
            # Assuming you have a starting number of subscribers and average revenue per user (ARPU):
            subs = [starting_subscribers]
            arpu = [starting_arpu]
            revenue = []
            for i, year in enumerate(years):
                if i > 0:
                    growth_rate = assumptions[scenario]['subscriber_growth'][i]
                    subs.append(subs[-1] * (1 + growth_rate))
                    arpu.append(arpu[-1] * (1 + assumptions[scenario]['arpu_growth'][i]))
                revenue.append(subs[-1] * arpu[-1])
            df['Subscribers'] = subs
            df['ARPU'] = arpu
            df['Revenue'] = revenue

            # Project Expenses
            marketing_expense_ratio = assumptions[scenario]['marketing_expense_ratio']
            cost_of_revenue_ratio = assumptions[scenario]['cost_of_revenue_ratio']
            df['Marketing Expense'] = df['Revenue'] * marketing_expense_ratio
            df['Cost of Revenue'] = df['Revenue'] * cost_of_revenue_ratio
            df['Operating Expenses'] = df['Marketing Expense'] + df['Cost of Revenue']
            
            # Calculate EBITDA and Net Income
            df['EBITDA'] = df['Revenue'] - df['Operating Expenses']
            # Assuming depreciation and amortization, interest, taxes are handled similarly
            depreciation = 0  # Placeholder
            interest = 0  # Placeholder
            taxes = 0  # Placeholder
            df['Net Income'] = df['EBITDA'] - depreciation - interest - taxes

            # Calculate Free Cash Flow
            capex = df['Revenue'] * 0.10  # Example: CapEx is 10% of revenue
            change_in_working_capital = 0  # Placeholder
            df['Free Cash Flow'] = df['Net Income'] + depreciation - capex - change_in_working_capital

            # Perform DCF Valuation
            # Calculate the present value of future cash flows.
            df['Discount Factor'] = [(1 / (1 + discount_rate)) ** i for i in range(len(years))]
            df['Discounted Cash Flow'] = df['Free Cash Flow'] * df['Discount Factor']
            npv = df['Discounted Cash Flow'].sum()
            terminal_value = df['Free Cash Flow'].iloc[-1] * (1 + 0.02) / (discount_rate - 0.02)
            terminal_value_discounted = terminal_value * df['Discount Factor'].iloc[-1]
            df['NPV'] = npv + terminal_value_discounted

            # Calculate Intrinsic Value and Compare to Share Price
            intrinsic_value = df['NPV'].iloc[-1]
            intrinsic_value_per_share = intrinsic_value / shares_outstanding
            premium_discount = (current_share_price - intrinsic_value_per_share) / intrinsic_value_per_share
            print(f"Scenario: {scenario}")
            print(f"Intrinsic Value per Share: ${intrinsic_value_per_share:.2f}")
            print(f"Premium/Discount: {premium_discount:.2%}")
            print()

            # Output the financials for each scenario
            summary.append({
                'Scenario': scenario,
                'Intrinsic Value per Share': intrinsic_value_per_share,
                'Premium/Discount': premium_discount
            })
            fig = px.line(df, 
                            title=f"Financial Projections - {scenario}",
                            labels={'index': 'Year', 'value': 'Amount ($)'},
                            template='plotly_dark',
                            width=800,
                            height=500,
                            x = df.index,
                            y=['Revenue', 'Operating Expenses']
                            )
            fig.show()

        return pd.DataFrame(summary)


    def project_subscribers(scenario_assumptions):
        # Code to project subscribers
        pass

    def project_financials(scenario, df, assumptions):
        # Code to project financials
        pass

    def perform_valuation(df):
        # Code to perform DCF valuation
        pass