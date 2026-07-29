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
    top: isMobile ? 0 : 10,
    bottom: isMobile ? 0 : 65 + additionalLegendMargin,
    left: isMobile ? 0 : 68,
    right: isMobile ? 0 : 40,
  },
});
