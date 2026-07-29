type MainDiagramLayoutOptions = {
  isMobile: boolean;
  isLargeScreen: boolean;
  additionalLegendMargin: number;
};

export const getMainDiagramLayout = ({
  isMobile,
  isLargeScreen,
  additionalLegendMargin,
}: MainDiagramLayoutOptions) => ({
  height: (isLargeScreen ? 700 : isMobile ? 400 : 450) + additionalLegendMargin,
  margin: {
    top: 10,
    bottom: (isMobile ? 42 : 65) + additionalLegendMargin,
    left: isMobile ? 44 : 68,
    right: isMobile ? 12 : 40,
  },
});
